#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <atomic>
#include <cstdint>
#include <cstdarg>
#include <cstring>
#include <memory>
#include <string>
#include <string_view>
#include <vector>

#include "vendor/IoStoreLoaderMod/minhook/include/MinHook.h"

namespace RC {
namespace GUI { class GUITab; }
namespace LuaMadeSimple { class Lua; }

using StringType = std::wstring;
using StringViewType = std::wstring_view;

// This declaration mirrors UE4SS 3.0.1's public C++ mod ABI. Only the base
// constructor/destructor are imported; the unused callbacks stay local.
class CppUserModBase {
protected:
    std::vector<std::shared_ptr<GUI::GUITab>> GUITabs{};
public:
    StringType ModName{};
    StringType ModVersion{};
    StringType ModDescription{};
    StringType ModAuthors{};
    StringType ModIntendedSDKVersion{};

    __declspec(dllimport) CppUserModBase();
    __declspec(dllimport) virtual ~CppUserModBase();
    virtual void on_update() {}
    virtual void on_unreal_init() {}
    virtual void on_ui_init() {}
    virtual void on_program_start() {}
    virtual void on_lua_start(StringViewType, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, std::vector<LuaMadeSimple::Lua*>&) {}
    virtual void on_lua_start(LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, std::vector<LuaMadeSimple::Lua*>&) {}
    virtual void on_lua_stop(StringViewType, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, std::vector<LuaMadeSimple::Lua*>&) {}
    virtual void on_lua_stop(LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, std::vector<LuaMadeSimple::Lua*>&) {}
    virtual void on_dll_load(StringViewType) {}
    virtual void render_tab() {}
    virtual void on_lua_start(StringViewType, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua*) {}
    virtual void on_lua_start(LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua*) {}
    virtual void on_lua_stop(StringViewType, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua*) {}
    virtual void on_lua_stop(LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua&, LuaMadeSimple::Lua*) {}
    virtual void on_cpp_mods_loaded() {}
};
} // namespace RC

namespace {
HMODULE g_module{};
HANDLE g_log{INVALID_HANDLE_VALUE};
std::atomic<long> g_mount_state{0};

enum class EIoErrorCode : int32_t {
    Ok = 0,
};

struct FIoStatus {
    EIoErrorCode ErrorCode{};
    wchar_t ErrorMessage[128]{};
};

struct FGuid {
    uint32_t A{}, B{}, C{}, D{};
};

struct FAESKey {
    uint8_t Key[32]{};
};

using IoMountFn = FIoStatus* (__fastcall*)(
    void* dispatcher,
    FIoStatus* result,
    const wchar_t* utoc_path,
    int32_t order,
    const FGuid* container_id,
    const FAESKey* encryption_key);

IoMountFn g_original_mount{};
std::wstring g_custom_utoc;
uint8_t* g_require_signature_branch{};
alignas(64) uint8_t g_custom_status_storage[4096]{};
alignas(16) FGuid g_custom_guid{};
alignas(16) FAESKey g_custom_key{};

void log_line(const wchar_t* format, ...) {
    if (g_log == INVALID_HANDLE_VALUE) return;
    wchar_t wide[4096]{};
    va_list args;
    va_start(args, format);
    _vsnwprintf_s(wide, _countof(wide), _TRUNCATE, format, args);
    va_end(args);
    wcscat_s(wide, L"\r\n");
    char utf8[16384]{};
    const int length = WideCharToMultiByte(CP_UTF8, 0, wide, -1, utf8,
                                            static_cast<int>(sizeof(utf8)), nullptr, nullptr);
    if (length > 1) {
        DWORD written{};
        WriteFile(g_log, utf8, static_cast<DWORD>(length - 1), &written, nullptr);
        FlushFileBuffers(g_log);
    }
}

std::wstring module_path() {
    std::wstring path(32768, L'\0');
    const DWORD length = GetModuleFileNameW(g_module, path.data(), static_cast<DWORD>(path.size()));
    path.resize(length);
    return path;
}

std::wstring parent_path(std::wstring path) {
    const size_t slash = path.find_last_of(L"\\/");
    if (slash == std::wstring::npos) return {};
    path.resize(slash);
    return path;
}

std::wstring find_payload_utoc(const std::wstring& payload_dir) {
    WIN32_FIND_DATAW data{};
    const std::wstring pattern = payload_dir + L"\\*.utoc";
    HANDLE find = FindFirstFileW(pattern.c_str(), &data);
    if (find == INVALID_HANDLE_VALUE) return payload_dir + L"\\AC8PGMDirect_P.utoc";
    std::wstring result = payload_dir + L"\\" + data.cFileName;
    FindClose(find);
    return result;
}

bool ends_with_ci(const wchar_t* value, const wchar_t* suffix) {
    if (!value || !suffix) return false;
    const size_t value_len = std::wcslen(value);
    const size_t suffix_len = std::wcslen(suffix);
    return value_len >= suffix_len &&
        _wcsicmp(value + value_len - suffix_len, suffix) == 0;
}

uint8_t* find_mount_function() {
    static constexpr uint8_t signature[] = {
        0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x57,
        0x48, 0x8D, 0xAC, 0x24, 0xF0, 0xFC, 0xFF, 0xFF,
        0x48, 0x81, 0xEC, 0x10, 0x04, 0x00, 0x00,
        0x48, 0x8B, 0x05
    };

    auto* base = reinterpret_cast<uint8_t*>(GetModuleHandleW(nullptr));
    if (!base) return nullptr;
    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS64*>(base + dos->e_lfanew);
    auto* section = IMAGE_FIRST_SECTION(nt);
    uint8_t* match = nullptr;
    size_t matches = 0;

    for (WORD i = 0; i < nt->FileHeader.NumberOfSections; ++i) {
        if ((section[i].Characteristics & IMAGE_SCN_MEM_EXECUTE) == 0) continue;
        auto* start = base + section[i].VirtualAddress;
        const size_t size = section[i].Misc.VirtualSize;
        if (size < sizeof(signature)) continue;
        for (size_t offset = 0; offset <= size - sizeof(signature); ++offset) {
            if (std::memcmp(start + offset, signature, sizeof(signature)) == 0) {
                match = start + offset;
                ++matches;
            }
        }
    }
    log_line(L"mount signature matches=%zu address=%p", matches, match);
    return matches == 1 ? match : nullptr;
}

uint8_t* find_require_signature_branch() {
    static constexpr uint8_t signature[] = {
        0xE8, 0, 0, 0, 0,
        0x44, 0x39, 0x70, 0x08, 0x74, 0x56,
        0x48, 0x8B, 0x08, 0x48, 0x85, 0xC9, 0x74, 0x4E,
        0x48, 0x8B, 0x01, 0xFF, 0x50, 0x30,
        0x84, 0xC0, 0x74, 0x44, 0x40, 0xF6, 0xC7, 0x04, 0x75, 0x48
    };
    static constexpr char mask[] = "x????xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

    auto* base = reinterpret_cast<uint8_t*>(GetModuleHandleW(nullptr));
    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS64*>(base + dos->e_lfanew);
    auto* section = IMAGE_FIRST_SECTION(nt);
    uint8_t* match = nullptr;
    size_t matches = 0;
    for (WORD i = 0; i < nt->FileHeader.NumberOfSections; ++i) {
        if ((section[i].Characteristics & IMAGE_SCN_MEM_EXECUTE) == 0) continue;
        auto* start = base + section[i].VirtualAddress;
        const size_t size = section[i].Misc.VirtualSize;
        for (size_t offset = 0; offset + sizeof(signature) <= size; ++offset) {
            bool equal = true;
            for (size_t byte = 0; byte < sizeof(signature); ++byte) {
                if (mask[byte] == 'x' && start[offset + byte] != signature[byte]) {
                    equal = false;
                    break;
                }
            }
            if (equal) {
                match = start + offset;
                ++matches;
            }
        }
    }
    // The conditional jump immediately after "test al, al" is byte 27.
    auto* branch = matches == 1 ? match + 27 : nullptr;
    log_line(L"signature requirement matches=%zu branch=%p", matches, branch);
    return branch;
}

bool patch_byte(uint8_t* address, uint8_t expected, uint8_t replacement) {
    if (!address || *address != expected) return false;
    DWORD old_protect{};
    if (!VirtualProtect(address, 1, PAGE_EXECUTE_READWRITE, &old_protect)) return false;
    *address = replacement;
    FlushInstructionCache(GetCurrentProcess(), address, 1);
    DWORD ignored{};
    VirtualProtect(address, 1, old_protect, &ignored);
    return true;
}

FIoStatus* __fastcall mount_hook(
    void* dispatcher,
    FIoStatus* result,
    const wchar_t* utoc_path,
    int32_t order,
    const FGuid* container_id,
    const FAESKey* encryption_key) {
    FIoStatus* returned = g_original_mount(
        dispatcher, result, utoc_path, order, container_id, encryption_key);

    const int code = returned ? static_cast<int>(returned->ErrorCode) : -1;
    log_line(L"stock mount code=%d order=%d path=%ls", code, order,
             utoc_path ? utoc_path : L"<null>");

    if (code == 0 && utoc_path &&
        ends_with_ci(utoc_path, L"pakchunk0-Windows.utoc")) {
        long expected = 0;
        if (g_mount_state.compare_exchange_strong(expected, 1)) {
            std::memset(g_custom_status_storage, 0xCD, sizeof(g_custom_status_storage));
            std::memset(g_custom_status_storage, 0, sizeof(FIoStatus));
            auto* custom_status = reinterpret_cast<FIoStatus*>(g_custom_status_storage);
            g_custom_guid = {};
            g_custom_key = {};
            log_line(L"mounting custom container order=1000 path=%ls", g_custom_utoc.c_str());
            const bool bypassed = patch_byte(g_require_signature_branch, 0x74, 0xEB);
            log_line(L"temporary unsigned-container bypass=%ls", bypassed ? L"on" : L"failed");
            FIoStatus* custom_result = bypassed
                ? g_original_mount(dispatcher, custom_status, g_custom_utoc.c_str(), 1000,
                                   &g_custom_guid, &g_custom_key)
                : custom_status;
            const bool restored = bypassed && patch_byte(g_require_signature_branch, 0xEB, 0x74);
            log_line(L"signature branch restored=%ls", restored ? L"yes" : L"no");
            size_t last_written = 0;
            for (size_t index = 0; index < sizeof(g_custom_status_storage); ++index) {
                if (g_custom_status_storage[index] != 0xCD) last_written = index;
            }
            log_line(L"status buffer last-written-offset=0x%zX", last_written);
            const int custom_code = custom_result ? static_cast<int>(custom_result->ErrorCode) : -1;
            log_line(L"custom mount code=%d message=%ls", custom_code,
                     custom_result ? custom_result->ErrorMessage : L"<null>");
            g_mount_state.store(custom_code == 0 ? 2 : -1);
        }
    }
    return returned;
}

bool initialize_loader() {
    const std::wstring dll_dir = parent_path(module_path());
    const std::wstring mod_dir = parent_path(dll_dir);
    g_custom_utoc = find_payload_utoc(mod_dir + L"\\AC8PGMDirect");
    const std::wstring log_path = mod_dir + L"\\AC8IoStoreLoader.log";
    g_log = CreateFileW(log_path.c_str(), GENERIC_WRITE,
                        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                        nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    log_line(L"AC8 IoStore loader 0.1.0");
    log_line(L"payload=%ls", g_custom_utoc.c_str());
    if (GetFileAttributesW(g_custom_utoc.c_str()) == INVALID_FILE_ATTRIBUTES) {
        log_line(L"payload .utoc is missing");
        return false;
    }

    auto* target = find_mount_function();
    g_require_signature_branch = find_require_signature_branch();
    if (!target || !g_require_signature_branch) return false;
    MH_STATUS status = MH_Initialize();
    if (status != MH_OK && status != MH_ERROR_ALREADY_INITIALIZED) {
        log_line(L"MH_Initialize failed: %S", MH_StatusToString(status));
        return false;
    }
    status = MH_CreateHook(target, reinterpret_cast<void*>(mount_hook),
                           reinterpret_cast<void**>(&g_original_mount));
    if (status != MH_OK) {
        log_line(L"MH_CreateHook failed: %S", MH_StatusToString(status));
        return false;
    }
    status = MH_EnableHook(target);
    log_line(L"MH_EnableHook status=%S", MH_StatusToString(status));
    return status == MH_OK;
}

class AC8IoStoreLoaderMod final : public RC::CppUserModBase {
public:
    AC8IoStoreLoaderMod() {
        ModName = L"IoStoreLoaderMod";
        ModVersion = L"0.1.0-ac8-1.1.2.0-ue5.4";
        ModDescription = L"Mounts AC8 PGM (Proportional Guided Missile) at startup";
        ModAuthors = L"Local AC8 adapter";
        initialize_loader();
    }

    ~AC8IoStoreLoaderMod() override {
        if (g_original_mount) MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
        if (g_log != INVALID_HANDLE_VALUE) {
            CloseHandle(g_log);
            g_log = INVALID_HANDLE_VALUE;
        }
    }
};
} // namespace

extern "C" __declspec(dllexport) RC::CppUserModBase* start_mod() {
    return new AC8IoStoreLoaderMod();
}

extern "C" __declspec(dllexport) void uninstall_mod(RC::CppUserModBase* mod) {
    delete mod;
}

BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        g_module = instance;
        DisableThreadLibraryCalls(instance);
    }
    return TRUE;
}
