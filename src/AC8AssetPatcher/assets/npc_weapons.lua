-- 由 tools/gen_npc_weapons.py 自动生成，请勿手改。
-- 数据来源：pakstore.json（与 weapon_classes.lua 同一出处）
--
-- 敌方武器：/Game/Blueprints/Weapons/MSL/NPC/<Family>/BP_wp_<name>
-- 与玩家侧（Player/ + BP_plwp_）是**各自独立的蓝图类**。
--
-- 分两张表：AIR 是对空导弹（默认纳入），OTHER 是地对空/舰载/
-- 支援类（默认不碰，要改得显式打开）。

-- 对空导弹（敌方）
NPC_AIR = {
    -- 2aam
    "/Game/Blueprints/Weapons/MSL/NPC/2aam/BP_wp_2aam_a0.Default__BP_wp_2aam_a0_C",
    -- 4aam
    "/Game/Blueprints/Weapons/MSL/NPC/4aam/BP_wp_4aam_a0.Default__BP_wp_4aam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/4aam/BP_wp_4aam_f0.Default__BP_wp_4aam_f0_C",
    -- 6aam
    "/Game/Blueprints/Weapons/MSL/NPC/6aam/BP_wp_6aam_a0.Default__BP_wp_6aam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/6aam/BP_wp_6aam_r0.Default__BP_wp_6aam_r0_C",
    -- 8aam
    "/Game/Blueprints/Weapons/MSL/NPC/8aam/BP_wp_8aam_a0.Default__BP_wp_8aam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/8aam/BP_wp_8aam_r0.Default__BP_wp_8aam_r0_C",
    -- hcaa
    "/Game/Blueprints/Weapons/MSL/NPC/Hcaa/BP_wp_hcaa_a0.Default__BP_wp_hcaa_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hcaa/BP_wp_hcaa_f0.Default__BP_wp_hcaa_f0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hcaa/BP_wp_hcaa_j0.Default__BP_wp_hcaa_j0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hcaa/BP_wp_hcaa_r0.Default__BP_wp_hcaa_r0_C",
    -- hpaa
    "/Game/Blueprints/Weapons/MSL/NPC/Hpaa/BP_wp_hpaa_a0.Default__BP_wp_hpaa_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hpaa/BP_wp_hpaa_j0.Default__BP_wp_hpaa_j0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hpaa/BP_wp_hpaa_r0.Default__BP_wp_hpaa_r0_C",
    -- hvaa
    "/Game/Blueprints/Weapons/MSL/NPC/Hvaa/BP_wp_hvaa_a0.Default__BP_wp_hvaa_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Hvaa/BP_wp_hvaa_r0.Default__BP_wp_hvaa_r0_C",
    -- laam
    "/Game/Blueprints/Weapons/MSL/NPC/Laam/BP_wp_laam_a0.Default__BP_wp_laam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Laam/BP_wp_laam_e0.Default__BP_wp_laam_e0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Laam/BP_wp_laam_r0.Default__BP_wp_laam_r0_C",
    -- msl
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_a0.Default__BP_wp_msl_Shadow_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_a1.Default__BP_wp_msl_Shadow_a1_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_f0.Default__BP_wp_msl_Shadow_f0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_j0.Default__BP_wp_msl_Shadow_j0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_r0.Default__BP_wp_msl_Shadow_r0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Shadow_s0.Default__BP_wp_msl_Shadow_s0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_Toplak.Default__BP_wp_msl_Toplak_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_a0.Default__BP_wp_msl_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_a1.Default__BP_wp_msl_a1_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_f0.Default__BP_wp_msl_f0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_j0.Default__BP_wp_msl_j0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_r0.Default__BP_wp_msl_r0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Msl/BP_wp_msl_s0.Default__BP_wp_msl_s0_C",
    -- qaam
    "/Game/Blueprints/Weapons/MSL/NPC/Qaam/BP_wp_qaam_a0.Default__BP_wp_qaam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Qaam/BP_wp_qaam_r0.Default__BP_wp_qaam_r0_C",
    -- saam
    "/Game/Blueprints/Weapons/MSL/NPC/Saam/BP_wp_saam_a0.Default__BP_wp_saam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Saam/BP_wp_saam_r0.Default__BP_wp_saam_r0_C",
    -- sasm
    "/Game/Blueprints/Weapons/MSL/NPC/Sasm/BP_wp_sasm_a0.Default__BP_wp_sasm_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sasm/BP_wp_sasm_r0.Default__BP_wp_sasm_r0_C",
}

-- 地对空 / 舰载 / 支援（默认不用）
NPC_OTHER = {
    -- 4agm
    "/Game/Blueprints/Weapons/MSL/NPC/4agm/BP_wp_4agm_a0.Default__BP_wp_4agm_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/4agm/BP_wp_4agm_r0.Default__BP_wp_4agm_r0_C",
    -- 6agm
    "/Game/Blueprints/Weapons/MSL/NPC/6agm/BP_wp_6agm_a0.Default__BP_wp_6agm_a0_C",
    -- 8agm
    "/Game/Blueprints/Weapons/MSL/NPC/8agm/BP_wp_8agm_a0.Default__BP_wp_8agm_a0_C",
    -- lacm
    "/Game/Blueprints/Weapons/MSL/NPC/Lacm/BP_wp_lacm_a0.Default__BP_wp_lacm_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lacm/BP_wp_lacm_f0.Default__BP_wp_lacm_f0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lacm/BP_wp_lacm_r0.Default__BP_wp_lacm_r0_C",
    -- lagm
    "/Game/Blueprints/Weapons/MSL/NPC/Lagm/BP_wp_lagm_a0.Default__BP_wp_lagm_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lagm/BP_wp_lagm_r0.Default__BP_wp_lagm_r0_C",
    -- lasm
    "/Game/Blueprints/Weapons/MSL/NPC/Lasm/BP_wp_lasm_a0.Default__BP_wp_lasm_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lasm/BP_wp_lasm_f0.Default__BP_wp_lasm_f0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lasm/BP_wp_lasm_j0.Default__BP_wp_lasm_j0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Lasm/BP_wp_lasm_r0.Default__BP_wp_lasm_r0_C",
    -- manpads
    "/Game/Blueprints/Weapons/MSL/NPC/Manpads/BP_wp_manpads_a0.Default__BP_wp_manpads_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Manpads/BP_wp_manpads_a0_minigame.Default__BP_wp_manpads_a0_minigame_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Manpads/BP_wp_manpads_certain_kill.Default__BP_wp_manpads_certain_kill_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Manpads/BP_wp_manpads_r0.Default__BP_wp_manpads_r0_C",
    -- mpbm
    "/Game/Blueprints/Weapons/MSL/NPC/Mpbm/BP_wp_mpbm_x0.Default__BP_wp_mpbm_x0_C",
    -- rkt
    "/Game/Blueprints/Weapons/MSL/NPC/Rkt/BP_wp_rkt_a0.Default__BP_wp_rkt_a0_C",
    -- sam
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_a0.Default__BP_wp_sam_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_a1.Default__BP_wp_sam_a1_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_certain_kill.Default__BP_wp_sam_certain_kill_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_e0.Default__BP_wp_sam_e0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_ldbs.Default__BP_wp_sam_ldbs_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_r0.Default__BP_wp_sam_r0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Sam/BP_wp_sam_r1.Default__BP_wp_sam_r1_C",
    -- ssm
    "/Game/Blueprints/Weapons/MSL/NPC/Ssm/BP_wp_ssm_a0.Default__BP_wp_ssm_a0_C",
    -- vls
    "/Game/Blueprints/Weapons/MSL/NPC/Vls/BP_wp_vls_BaseClass.Default__BP_wp_vls_BaseClass_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Vls/BP_wp_vls_a0.Default__BP_wp_vls_a0_C",
    "/Game/Blueprints/Weapons/MSL/NPC/Vls/BP_wp_vls_r0.Default__BP_wp_vls_r0_C",
}

return {
    NPC_AIR   = NPC_AIR,
    NPC_OTHER = NPC_OTHER,
}
