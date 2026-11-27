using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Plutonium T6 Mod & Pack Manager")]
[assembly: AssemblyDescription("Automated Mod & Pack Manager for Call of Duty: Black Ops II (Plutonium T6)")]
[assembly: AssemblyCompany("NicolasPaiao2")]
[assembly: AssemblyProduct("Plutonium T6 Mod & Pack Manager")]
[assembly: AssemblyVersion("4.5.1.0")]
[assembly: AssemblyFileVersion("4.5.1.0")]

namespace PlutoniumT6ModManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            GitHubUpdateService.CleanupOldUpdateArtifacts();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public enum ModTargetType
    {
        MainIwd = 0,
        ScriptsZm = 1,
        ScriptsMp = 2,
        ScriptsRoot = 3,
        Usermap = 4,
        ModFastFile = 5,
        CustomMapMod = 6
    }

    [DataContract]
    public class ModEntryItem
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public string VaultSubFolder { get; set; }
        [DataMember] public int TargetTypeInt { get; set; }
        [DataMember] public long SizeBytes { get; set; }
        [DataMember] public int FileCount { get; set; }
        [DataMember] public string PreviewPath { get; set; }

        [DataMember(IsRequired = false)]
        public string DetectedMode { get; set; }

        [DataMember(IsRequired = false)]
        public string DetectedSummaryPt { get; set; }

        [DataMember(IsRequired = false)]
        public string DetectedSummaryEn { get; set; }

        [DataMember(IsRequired = false)]
        public string DetectedItemsList { get; set; }

        public ModTargetType TargetType
        {
            get { return (ModTargetType)TargetTypeInt; }
            set { TargetTypeInt = (int)value; }
        }
    }

    public static class ModContentAnalyzer
    {
        private class TextureRule
        {
            public string[] Tokens;
            public string LabelPt;
            public string LabelEn;
            public string Mode; // "MP", "ZM", "MP/ZM"

            public TextureRule(string[] tokens, string labelPt, string labelEn, string mode)
            {
                Tokens = tokens;
                LabelPt = labelPt;
                LabelEn = labelEn;
                Mode = mode;
            }
        }

        private static readonly TextureRule[] Rules = new TextureRule[]
        {
            // Zombies-specific Pack-a-Punch & Zombies maps/camos
            new TextureRule(new string[] { "zmb_pap", "zm_pap", "wpn_zmb_pap", "pap_camo" }, "Camo: Pack-a-Punch (ZM)", "Camo: Pack-a-Punch (ZM)", "ZM"),
            new TextureRule(new string[] { "alcatraz", "zm_prison", "zmb_prison" }, "Camo/Tex: Mob of the Dead / Afterlife", "Camo/Tex: Mob of the Dead / Afterlife", "ZM"),
            new TextureRule(new string[] { "zm_tomb", "zmb_tomb", "origins_camo" }, "Camo/Tex: Origins (Element 115)", "Camo/Tex: Origins (Element 115)", "ZM"),
            new TextureRule(new string[] { "zm_buried", "zmb_buried" }, "Tex: Buried (ZM)", "Tex: Buried (ZM)", "ZM"),
            new TextureRule(new string[] { "zm_highrise", "zmb_highrise" }, "Tex: Die Rise (ZM)", "Tex: Die Rise (ZM)", "ZM"),
            new TextureRule(new string[] { "zm_transit", "zmb_transit" }, "Tex: TranZit (ZM)", "Tex: TranZit (ZM)", "ZM"),
            new TextureRule(new string[] { "zm_nuked", "zmb_nuked" }, "Tex: Nuketown Zombies", "Tex: Nuketown Zombies", "ZM"),
            new TextureRule(new string[] { "specialty_", "vending_", "juggernog", "speedcola", "doubletap", "quickrevive", "electric_cherry", "mulekick", "staminup", "phd", "vulture", "whoswho" }, "Perks / Maquinas (ZM)", "Perks / Machines (ZM)", "ZM"),
            new TextureRule(new string[] { "raygun", "ray_gun", "mark2", "blundergat", "acidgat", "staff_water", "staff_fire", "staff_wind", "staff_lightning" }, "Wonder Weapons (ZM)", "Wonder Weapons (ZM)", "ZM"),

            // Standard & DLC Black Ops II Camos (MP / Universal)
            new TextureRule(new string[] { "camo_01", "devgru" }, "Camo: DEVGRU", "Camo: DEVGRU", "MP"),
            new TextureRule(new string[] { "camo_02", "a_tacs", "atacs" }, "Camo: A-TACS AU", "Camo: A-TACS AU", "MP"),
            new TextureRule(new string[] { "camo_03", "erdl" }, "Camo: ERDL", "Camo: ERDL", "MP"),
            new TextureRule(new string[] { "camo_04", "siberia" }, "Camo: Siberia", "Camo: Siberia", "MP"),
            new TextureRule(new string[] { "camo_05", "choco" }, "Camo: Choco", "Camo: Choco", "MP"),
            new TextureRule(new string[] { "camo_06", "blue_tiger" }, "Camo: Blue Tiger", "Camo: Blue Tiger", "MP"),
            new TextureRule(new string[] { "camo_07", "bloodshot" }, "Camo: Bloodshot", "Camo: Bloodshot", "MP"),
            new TextureRule(new string[] { "camo_08", "ghostex", "delta6" }, "Camo: Ghostex Delta 6", "Camo: Ghostex Delta 6", "MP"),
            new TextureRule(new string[] { "camo_09", "kryptek", "typhon" }, "Camo: Kryptek Typhon", "Camo: Kryptek Typhon", "MP"),
            new TextureRule(new string[] { "camo_10", "carbon" }, "Camo: Carbon Fiber", "Camo: Carbon Fiber", "MP"),
            new TextureRule(new string[] { "camo_11", "cherry" }, "Camo: Cherry Blossom", "Camo: Cherry Blossom", "MP"),
            new TextureRule(new string[] { "camo_12", "art_of_war" }, "Camo: Art of War", "Camo: Art of War", "MP"),
            new TextureRule(new string[] { "camo_13", "ronin" }, "Camo: Ronin", "Camo: Ronin", "MP"),
            new TextureRule(new string[] { "camo_14", "skulls" }, "Camo: Skulls", "Camo: Skulls", "MP"),
            new TextureRule(new string[] { "camo_15", "_gold", "camo_gold" }, "Camo: Ouro (Gold)", "Camo: Gold", "MP"),
            new TextureRule(new string[] { "camo_16", "diamond" }, "Camo: Diamante (Diamond)", "Camo: Diamond", "MP"),
            new TextureRule(new string[] { "camo_17", "ce_digital", "camo_elite" }, "Camo: CE Digital", "Camo: CE Digital", "MP"),
            new TextureRule(new string[] { "camo_18", "ghosts" }, "Camo: Ghosts", "Camo: Ghosts", "MP"),
            new TextureRule(new string[] { "camo_19", "jungle" }, "Camo: Jungle Warfare", "Camo: Jungle Warfare", "MP"),
            new TextureRule(new string[] { "camo_20", "benjamins" }, "Camo: Benjamins", "Camo: Benjamins", "MP"),
            new TextureRule(new string[] { "camo_21", "muertos" }, "Camo: Dia de Muertos", "Camo: Dia de Muertos", "MP"),
            new TextureRule(new string[] { "camo_22", "graffiti" }, "Camo: Graffiti", "Camo: Graffiti", "MP"),
            new TextureRule(new string[] { "camo_23", "kawaii" }, "Camo: Kawaii", "Camo: Kawaii", "MP"),
            new TextureRule(new string[] { "camo_24", "party_rock" }, "Camo: Party Rock", "Camo: Party Rock", "MP"),
            new TextureRule(new string[] { "camo_25", "camo_zombies" }, "Camo: Zombies", "Camo: Zombies", "MP"),
            new TextureRule(new string[] { "camo_26", "viper" }, "Camo: Viper", "Camo: Viper", "MP"),
            new TextureRule(new string[] { "camo_27", "bacon" }, "Camo: Bacon", "Camo: Bacon", "MP"),
            new TextureRule(new string[] { "camo_28", "cyborg" }, "Camo: Cyborg", "Camo: Cyborg", "MP"),
            new TextureRule(new string[] { "camo_29", "dragon" }, "Camo: Dragon", "Camo: Dragon", "MP"),
            new TextureRule(new string[] { "camo_30", "aqua" }, "Camo: Aqua", "Camo: Aqua", "MP"),
            new TextureRule(new string[] { "camo_31", "breach" }, "Camo: Breach", "Camo: Breach", "MP"),
            new TextureRule(new string[] { "camo_32", "coyote" }, "Camo: Coyote", "Camo: Coyote", "MP"),
            new TextureRule(new string[] { "camo_33", "glam" }, "Camo: Glam", "Camo: Glam", "MP"),
            new TextureRule(new string[] { "camo_34", "rogue" }, "Camo: Rogue", "Camo: Rogue", "MP"),
            new TextureRule(new string[] { "camo_35", "packapunch", "_pap_" }, "Camo: Pack-a-Punch", "Camo: Pack-a-Punch", "MP/ZM"),
            new TextureRule(new string[] { "camo_36", "weaponized", "_115" }, "Camo: Weaponized 115", "Camo: Weaponized 115", "MP"),
            new TextureRule(new string[] { "camo_37", "afterlife" }, "Camo: Afterlife", "Camo: Afterlife", "MP/ZM"),
            new TextureRule(new string[] { "camo_38", "octane" }, "Camo: Octane", "Camo: Octane", "MP"),
            new TextureRule(new string[] { "camo_39", "beast" }, "Camo: Beast", "Camo: Beast", "MP"),
            new TextureRule(new string[] { "camo_40", "uk_punk", "punk" }, "Camo: UK Punk", "Camo: UK Punk", "MP"),
            new TextureRule(new string[] { "camo_41", "comics" }, "Camo: Comics", "Camo: Comics", "MP"),
            new TextureRule(new string[] { "camo_42", "paladin" }, "Camo: Paladin", "Camo: Paladin", "MP"),
            new TextureRule(new string[] { "camo_43", "dead_mans" }, "Camo: Dead Man's Hand", "Camo: Dead Man's Hand", "MP"),
            new TextureRule(new string[] { "camo_44", "dragon_fire" }, "Camo: Dragon Fire", "Camo: Dragon Fire", "MP"),
            new TextureRule(new string[] { "camo_45", "camo_aw" }, "Camo: Advanced Warfare", "Camo: Advanced Warfare", "MP"),

            // Viewhands / Gloves
            new TextureRule(new string[] { "viewhands", "glove", "hands" }, "Luvas / Maos (Viewhands)", "Gloves / Viewhands", "MP/ZM"),

            // Reticles / Sights
            new TextureRule(new string[] { "reticle", "reflex", "eotech", "acog", "target_finder", "dual_band", "mms_" }, "Reticulas / Miras", "Reticles / Sights", "MP/ZM"),

            // Specific Weapons
            new TextureRule(new string[] { "dsr50" }, "Arma: DSR 50", "Weapon: DSR 50", "MP/ZM"),
            new TextureRule(new string[] { "ballista" }, "Arma: Ballista", "Weapon: Ballista", "MP/ZM"),
            new TextureRule(new string[] { "an94" }, "Arma: AN-94", "Weapon: AN-94", "MP/ZM"),
            new TextureRule(new string[] { "msmc" }, "Arma: MSMC", "Weapon: MSMC", "MP/ZM"),
            new TextureRule(new string[] { "pdw57" }, "Arma: PDW-57", "Weapon: PDW-57", "MP/ZM"),
            new TextureRule(new string[] { "mp7" }, "Arma: MP7", "Weapon: MP7", "MP/ZM"),
            new TextureRule(new string[] { "scar" }, "Arma: SCAR-H", "Weapon: SCAR-H", "MP/ZM"),
            new TextureRule(new string[] { "m8a1" }, "Arma: M8A1", "Weapon: M8A1", "MP/ZM"),
            new TextureRule(new string[] { "r870" }, "Arma: R870 MCS", "Weapon: R870 MCS", "MP/ZM"),
            new TextureRule(new string[] { "ksg" }, "Arma: KSG", "Weapon: KSG", "MP/ZM"),
            new TextureRule(new string[] { "b23r" }, "Arma: B23R", "Weapon: B23R", "MP/ZM"),
            new TextureRule(new string[] { "kap40" }, "Arma: KAP-40", "Weapon: KAP-40", "MP/ZM"),
            new TextureRule(new string[] { "executioner" }, "Arma: Executioner", "Weapon: Executioner", "MP/ZM"),
            new TextureRule(new string[] { "tomahawk", "knife" }, "Arma: Faca / Tomahawk", "Weapon: Knife / Tomahawk", "MP/ZM"),

            // Environment / HUD
            new TextureRule(new string[] { "sky_", "_sky", "sun_" }, "Ceu / Iluminacao", "Sky / Lighting", "MP/ZM"),
            new TextureRule(new string[] { "hud_", "menu_", "lui_" }, "Interface / HUD", "Interface / HUD", "MP/ZM")
        };

        public static void AnalyzeVaultItem(string itemVaultDir, ModEntryItem item)
        {
            if (item == null) return;

            List<string> inspectedNames = new List<string>();
            if (!string.IsNullOrEmpty(item.DisplayName))
                inspectedNames.Add(item.DisplayName.ToLowerInvariant());

            if (!string.IsNullOrEmpty(itemVaultDir) && Directory.Exists(itemVaultDir))
            {
                try
                {
                    string[] files = Directory.GetFiles(itemVaultDir, "*.*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string f = files[i];
                        string rel = f.Substring(itemVaultDir.Length).ToLowerInvariant();
                        inspectedNames.Add(rel);

                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".iwd" || ext == ".zip")
                        {
                            try
                            {
                                using (ZipArchive arc = ZipFile.OpenRead(f))
                                {
                                    foreach (ZipArchiveEntry entry in arc.Entries)
                                    {
                                        if (!string.IsNullOrEmpty(entry.FullName))
                                            inspectedNames.Add(entry.FullName.ToLowerInvariant());
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }

            List<string> foundPt = new List<string>();
            List<string> foundEn = new List<string>();
            bool hasZm = false;
            bool hasMp = false;

            if (item.TargetType == ModTargetType.ScriptsZm) hasZm = true;
            else if (item.TargetType == ModTargetType.ScriptsMp) hasMp = true;
            else if (item.TargetType == ModTargetType.CustomMapMod || item.TargetType == ModTargetType.Usermap)
            {
                string dn = (item.DisplayName ?? "").ToLowerInvariant();
                if (dn.StartsWith("zm_") || dn.Contains("zombie")) hasZm = true;
                else if (dn.StartsWith("mp_")) hasMp = true;
            }

            for (int i = 0; i < inspectedNames.Count; i++)
            {
                string name = inspectedNames[i].Replace('\\', '/');
                if (name.Contains("/zm/") || name.Contains("zm_") || name.Contains("zmb_") || name.Contains("zombie"))
                    hasZm = true;
                if (name.Contains("/mp/") || name.Contains("mp_"))
                    hasMp = true;

                for (int r = 0; r < Rules.Length; r++)
                {
                    TextureRule rule = Rules[r];
                    bool matched = false;
                    for (int t = 0; t < rule.Tokens.Length; t++)
                    {
                        if (name.Contains(rule.Tokens[t]))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        if (!foundPt.Contains(rule.LabelPt))
                        {
                            foundPt.Add(rule.LabelPt);
                            foundEn.Add(rule.LabelEn);
                        }
                        if (rule.Mode == "ZM") hasZm = true;
                        else if (rule.Mode == "MP") hasMp = true;
                    }
                }
            }

            // Determine Mode Tag
            string mode = "MP/ZM";
            if (hasZm && !hasMp) mode = "ZM";
            else if (hasMp && !hasZm) mode = "MP";
            else if (item.TargetType == ModTargetType.ScriptsZm) mode = "ZM";
            else if (item.TargetType == ModTargetType.ScriptsMp) mode = "MP";

            item.DetectedMode = mode;

            if (foundPt.Count > 0)
            {
                if (foundPt.Count == 1)
                {
                    item.DetectedSummaryPt = foundPt[0];
                    item.DetectedSummaryEn = foundEn[0];
                }
                else
                {
                    item.DetectedSummaryPt = foundPt[0] + " (+" + (foundPt.Count - 1) + ")";
                    item.DetectedSummaryEn = foundEn[0] + " (+" + (foundEn.Count - 1) + ")";
                }
                item.DetectedItemsList = string.Join(" | ", foundPt.ToArray());
            }
            else
            {
                // Fallback based on target type
                switch (item.TargetType)
                {
                    case ModTargetType.MainIwd:
                        item.DetectedSummaryPt = "Textura / Camo (.iwd)";
                        item.DetectedSummaryEn = "Texture / Camo (.iwd)";
                        break;
                    case ModTargetType.ScriptsZm:
                        item.DetectedSummaryPt = "Script Zombies (.gsc)";
                        item.DetectedSummaryEn = "Zombies Script (.gsc)";
                        break;
                    case ModTargetType.ScriptsMp:
                        item.DetectedSummaryPt = "Script Multiplayer (.gsc)";
                        item.DetectedSummaryEn = "Multiplayer Script (.gsc)";
                        break;
                    case ModTargetType.ScriptsRoot:
                        item.DetectedSummaryPt = "Script Geral (.gsc)";
                        item.DetectedSummaryEn = "General Script (.gsc)";
                        break;
                    case ModTargetType.CustomMapMod:
                    case ModTargetType.Usermap:
                        item.DetectedSummaryPt = "Mapa Customizado";
                        item.DetectedSummaryEn = "Custom Map";
                        break;
                    default:
                        item.DetectedSummaryPt = "Mod / FastFile";
                        item.DetectedSummaryEn = "Mod / FastFile";
                        break;
                }
                item.DetectedItemsList = "";
            }
        }
    }

    [DataContract]
    public class ModProfile
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string CreatedAt { get; set; }
        [DataMember] public List<ModEntryItem> Items { get; set; }

        public ModProfile()
        {
            Items = new List<ModEntryItem>();
        }
    }

    [DataContract]
    public class ManagerDatabase
    {
        [DataMember] public string Language { get; set; }
        [DataMember] public string T6Directory { get; set; }
        [DataMember] public string ActiveProfileId { get; set; }
        [DataMember] public List<ModProfile> Profiles { get; set; }

        public ManagerDatabase()
        {
            Profiles = new List<ModProfile>();
        }
    }

    public enum DiskSyncState
    {
        Clean = 0,
        SyncedOk = 1,
        PartiallyMissing = 2,
        AllMissing = 3
    }

    public class MainForm : Form
    {
        private readonly Color BgPrimary = Color.FromArgb(20, 21, 25);
        private readonly Color BgCard = Color.FromArgb(29, 31, 38);
        private readonly Color BgInput = Color.FromArgb(37, 40, 50);
        private readonly Color BorderSubtle = Color.FromArgb(52, 56, 70);
        private readonly Color AccentOrange = Color.FromArgb(245, 130, 13);
        private readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private readonly Color AccentCyan = Color.FromArgb(56, 189, 248);
        private readonly Color AccentPurple = Color.FromArgb(192, 132, 252);
        private readonly Color AccentCoral = Color.FromArgb(251, 113, 133);
        private readonly Color AccentAmber = Color.FromArgb(251, 191, 36);
        private readonly Color TextLight = Color.FromArgb(243, 244, 246);
        private readonly Color TextMuted = Color.FromArgb(156, 163, 175);

        private string vaultRoot;
        private string profilesVaultDir;
        private string dbFilePath;
        private string activeManifestPath;
        private ManagerDatabase db;

        private DiskSyncState currentSyncState = DiskSyncState.Clean;
        private HashSet<string> missingItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Label lblActiveBanner;
        private TextBox txtT6Path;
        private Button btnBrowseT6;
        private Button btnOpenT6;
        private Button btnLaunchPluto;
        private Button btnCheckSync;

        private Label lblStep1;
        private Label lblStep2;
        private Label lblStep3;

        private ListBox lstProfiles;
        private Button btnNewProfile;
        private Button btnRenameProfile;
        private Button btnDuplicateProfile;
        private Button btnDeleteProfile;

        private Label lblSelectedProfileTitle;
        private Label lblSelectedProfileStats;
        private Panel pnlMapNotice;
        private Label lblMapNotice;
        private ListBox lstModItems;
        private Button btnAddFiles;
        private Button btnAddFolder;
        private Button btnChangeDest;
        private Button btnRemoveItem;

        private ProgressBar progressBar;
        private Label lblStatus;
        private RichTextBox rtbLog;

        private Button btnActivateProfile;
        private Button btnDeactivateAll;
        private Button btnExportExe;

        private ContextMenuStrip ctxDestMenu;
        private ToolTip mainToolTip;

        static readonly string[] AssetFolderNames = new string[]
        {
            "images", "sound", "sounds", "weapons", "materials", "fx", "fonts", "vision", "sun"
        };

        private string currentLang = "pt";
        private Button btnCheckUpdate;
        private Button btnLangToggle;
        private Button btnHelpGuide;
        private const string CurrentAppVersion = "4.5.1";
        private const string AuthorName = "NicolasPaiao2";
        private const string GitHubOwner = "NicolasPaiao2";
        private const string GitHubRepoName = "Plutonium-T6-Mod-Pack-Manager";
        private const string GitHubRepoUrl = "https://github.com/NicolasPaiao2/Plutonium-T6-Mod-Pack-Manager";
        private UpdateReleaseInfo availableUpdateInfo = null;
        private bool isCheckingUpdate = false;

        private Label lblProfilesHdrRef;
        private Label lblProfilesSubRef;
        private Label lblT6Ref;
        private Label lblSubRef;
        private Label lblCreditFooter;
        private LinkLabel lnkGitHubFooter;

        private bool IsEn
        {
            get { return string.Equals(currentLang, "en", StringComparison.OrdinalIgnoreCase); }
        }

        private string L(string key)
        {
            bool en = IsEn;
            switch (key)
            {
                case "win_title":
                    return "Plutonium T6 Mod & Pack Manager v" + CurrentAppVersion + " - BO2 (main / scripts / mods)";
                case "header_sub":
                    return en
                        ? "Organize camos, scripts, and custom maps into packs and switch with 1 click (main / scripts / mods)"
                        : "Organize camuflagens, scripts e mapas customizados em packs e alterne com 1 clique (main / scripts / mods)";
                case "t6_label":
                    return en ? "Plutonium T6 Folder:" : "Pasta Plutonium T6:";
                case "browse":
                    return en ? "Browse..." : "Alterar...";
                case "open_t6":
                    return en ? "Open T6 Folder" : "Abrir Pasta T6";
                case "launch_pluto":
                    return en ? "Launch Plutonium" : "Abrir Plutonium";
                case "verify_t6":
                    return en ? "Verify T6" : "Verificar T6";
                case "profiles_hdr":
                    return en ? "MOD PACKS / PROFILES" : "LISTAS / PERFIS DE MODS";
                case "profiles_sub":
                    return en ? "Select a pack to edit or activate:" : "Selecione um pack para editar ou ativar:";
                case "new_profile":
                    return en ? "+ New Pack" : "+ Novo Perfil";
                case "rename_profile":
                    return en ? "Rename" : "Renomear";
                case "dup_profile":
                    return en ? "Duplicate" : "Duplicar";
                case "del_profile":
                    return en ? "Delete" : "Excluir";
                case "add_files":
                    return en ? "+ Files" : "+ Arquivos";
                case "add_folder":
                    return en ? "+ Mod / Map Folder" : "+ Pasta de Mod / Mapa";
                case "change_dest":
                    return en ? "Change Target" : "Alterar Destino";
                case "remove_item":
                    return en ? "Remove Selected" : "Remover Selecionado";
                case "activate":
                    return en ? "ACTIVATE PROFILE IN GAME" : "ATIVAR PERFIL NO JOGO";
                case "deactivate":
                    return en ? "Disable All (Clean Game)" : "Desativar Tudo (Jogo Limpo)";
                case "export_exe":
                    return en ? "Export Pack as Installer (.exe)" : "Exportar Perfil como Instalador (.exe)";
                case "default_hint":
                    return en
                        ? "Tip: Drag & drop .iwd, .iwi, .gsc, .zip files or custom map/mod folders here, then click ACTIVATE PROFILE IN GAME."
                        : "Dica: Arraste arquivos (.iwd, .iwi, .gsc, .zip) ou pastas de mapas/mods aqui e clique em ATIVAR PERFIL NO JOGO.";
                case "status_vanilla":
                    return en ? "STATUS: CLEAN GAME (VANILLA)" : "STATUS: JOGO LIMPO (VANILLA)";
                case "status_active":
                    return en ? "ACTIVE IN GAME: " : "ATIVO NO JOGO: ";
                case "status_partial":
                    return en ? "ACTIVE (PARTIAL - FILES MISSING IN T6): " : "ATIVO (PARCIAL - ARQUIVOS REMOVIDOS DE T6): ";
                case "credit_footer":
                    return en
                        ? "Created by " + AuthorName + "  |  v" + CurrentAppVersion + " (Official GitHub)"
                        : "Desenvolvido por " + AuthorName + "  |  v" + CurrentAppVersion + " (GitHub Oficial)";
                case "github_link":
                    return en ? "GitHub Page (NicolasPaiao2)" : "P\u00e1gina do GitHub (NicolasPaiao2)";
                case "update_btn":
                    if (availableUpdateInfo != null)
                        return en ? "Update " + availableUpdateInfo.TagName + "!" : "Atualizar " + availableUpdateInfo.TagName + "!";
                    return en ? "Updates" : "Atualiza\u00e7\u00f5es";
                case "guide_btn":
                    return en ? "Quick Guide" : "Guia R\u00e1pido";
                case "guide_title":
                    return en ? "Quick Start Guide - Plutonium T6 Mod Manager" : "Guia R\u00e1pido - Plutonium T6 Mod Manager";
                case "map_notice_inline":
                    return en
                        ? "Custom Map (ZM / MP): Installed to storage\\t6\\mods\\<map>. In-game, open \"Mods\" on the Zombies or Multiplayer main menu and load it first."
                        : "Mapa Customizado (ZM / MP): Instalado em storage\\t6\\mods\\<mapa>. No jogo, abra \"Mods\" no menu principal do Zombies ou Multiplayer e ative-o.";
                case "guide_body":
                    return en
                        ? "HOW IT WORKS (3 SIMPLE STEPS):\n\n" +
                          "1. PACKS / PROFILES (Left): Group camos, scripts, or custom maps into separate lists (e.g. Anime Pack, Custom Zombies).\n\n" +
                          "2. ADDING MODS & MAPS (Center): Click '+ Files' or '+ Mod / Map Folder' (or drag & drop). A safe copy is saved in your local Vault so you can delete your Downloads anytime.\n\n" +
                          "3. ACTIVATE IN GAME (Bottom Green Button): Click 'ACTIVATE PROFILE IN GAME' to copy files into storage\\t6 (main, scripts, mods).\n\n" +
                          "IMPORTANT FOR CUSTOM MAPS (ZOMBIES & MULTIPLAYER):\n" +
                          "Plutonium T6 requires custom maps to be placed in storage\\t6\\mods\\<map_name> and loaded via the in-game 'Mods' menu (on the Zombies or Multiplayer main menu) before starting a match.\n\n" +
                          "AUTO-SYNC CHECK:\n" +
                          "If you manually delete mod/map files from storage\\t6, the manager automatically detects it and updates the status."
                        : "COMO FUNCIONA (3 PASSOS SIMPLES):\n\n" +
                          "1. PERFIS / PACKS (Esquerda): Separe camuflagens, scripts ou mapas customizados em listas (ex: Pack Anime, Zombies Custom).\n\n" +
                          "2. ADICIONAR MODS E MAPAS (Centro): Use '+ Arquivos', '+ Pasta de Mod / Mapa' ou arraste para a lista. O programa guarda uma c\u00f3pia segura no Vault (voc\u00ea pode apagar os arquivos da pasta Downloads depois).\n\n" +
                          "3. ATIVAR NO JOGO (Bot\u00e3o Verde): Clique em 'ATIVAR PERFIL NO JOGO' para enviar os arquivos para storage\\t6 (main, scripts, mods).\n\n" +
                          "IMPORTANTE SOBRE MAPAS CUSTOMIZADOS (ZOMBIES E MULTIPLAYER):\n" +
                          "No Plutonium T6, mapas customizados precisam ficar em storage\\t6\\mods\\<nome_do_mapa> e ser carregados no menu 'Mods' (no menu principal do Zombies ou Multiplayer) antes de iniciar a partida.\n\n" +
                          "VERIFICA\u00c7\u00c3O AUTOM\u00c1TICA:\n" +
                          "Se voc\u00ea apagar manualmente algum mod ou mapa de dentro da pasta storage\\t6, o gerenciador detecta na hora e atualiza o status.";
                default:
                    return key;
            }
        }

        private void ToggleLanguage()
        {
            currentLang = IsEn ? "pt" : "en";
            db.Language = currentLang;
            SaveDatabase();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool en = IsEn;
            this.Text = L("win_title");
            if (lblSubRef != null)
                lblSubRef.Text = L("header_sub");
            if (btnCheckUpdate != null)
                btnCheckUpdate.Text = L("update_btn");
            if (btnCheckSync != null)
                btnCheckSync.Text = L("verify_t6");
            if (btnLangToggle != null)
                btnLangToggle.Text = en ? "PT / [EN]" : "[PT] / EN";
            if (btnHelpGuide != null)
                btnHelpGuide.Text = L("guide_btn");
            if (lblT6Ref != null)
                lblT6Ref.Text = L("t6_label");
            if (btnBrowseT6 != null)
                btnBrowseT6.Text = L("browse");
            if (btnOpenT6 != null)
                btnOpenT6.Text = L("open_t6");
            if (btnLaunchPluto != null)
                btnLaunchPluto.Text = L("launch_pluto");
            if (lblProfilesHdrRef != null)
                lblProfilesHdrRef.Text = L("profiles_hdr");
            if (lblProfilesSubRef != null)
                lblProfilesSubRef.Text = L("profiles_sub");
            if (btnNewProfile != null)
                btnNewProfile.Text = L("new_profile");
            if (btnRenameProfile != null)
                btnRenameProfile.Text = L("rename_profile");
            if (btnDuplicateProfile != null)
                btnDuplicateProfile.Text = L("dup_profile");
            if (btnDeleteProfile != null)
                btnDeleteProfile.Text = L("del_profile");
            if (btnAddFiles != null)
                btnAddFiles.Text = L("add_files");
            if (btnAddFolder != null)
                btnAddFolder.Text = L("add_folder");
            if (btnChangeDest != null)
                btnChangeDest.Text = L("change_dest");
            if (btnRemoveItem != null)
                btnRemoveItem.Text = L("remove_item");
            if (btnActivateProfile != null)
                btnActivateProfile.Text = L("activate");
            if (btnDeactivateAll != null)
                btnDeactivateAll.Text = L("deactivate");
            if (btnExportExe != null)
                btnExportExe.Text = L("export_exe");
            if (lblCreditFooter != null)
                lblCreditFooter.Text = L("credit_footer");
            if (lnkGitHubFooter != null)
                lnkGitHubFooter.Text = L("github_link");
            if (lblMapNotice != null)
                lblMapNotice.Text = L("map_notice_inline");
            if (lblStatus != null && string.IsNullOrEmpty(db.ActiveProfileId))
                lblStatus.Text = L("default_hint");

            if (ctxDestMenu != null && ctxDestMenu.Items.Count >= 6)
            {
                ctxDestMenu.Items[0].Text = en ? "main (Camos / Textures / IWD)" : "main (Camuflagens / Texturas / IWD)";
                ctxDestMenu.Items[1].Text = en ? "scripts\\zm (Zombies Scripts)" : "scripts\\zm (Scripts Zombies)";
                ctxDestMenu.Items[2].Text = en ? "scripts\\mp (Multiplayer Scripts)" : "scripts\\mp (Scripts Multiplayer)";
                ctxDestMenu.Items[3].Text = en ? "scripts (Scripts Root)" : "scripts (Raiz de Scripts)";
                ctxDestMenu.Items[4].Text = en ? "mods\\<map> (Custom Map ZM / MP)" : "mods\\<mapa> (Mapa Customizado ZM / MP)";
                ctxDestMenu.Items[5].Text = en ? "mods\\<mod> (FastFile mod.ff)" : "mods\\<mod> (FastFile mod.ff)";
            }

            SetupTooltips();
            UpdateActiveBanner();
            RefreshSelectedProfileView();
        }

        private void SetupTooltips()
        {
            if (mainToolTip == null) return;
            bool en = IsEn;
            if (btnCheckUpdate != null)
            {
                mainToolTip.SetToolTip(btnCheckUpdate, en
                    ? "Check GitHub (NicolasPaiao2) for updates. Current version: v" + CurrentAppVersion
                    : "Verifica atualiza\u00e7\u00f5es no GitHub (NicolasPaiao2). Vers\u00e3o atual: v" + CurrentAppVersion);
            }
            mainToolTip.SetToolTip(btnCheckSync, en
                ? "Check storage\\t6 on disk right now to verify if active mods/maps are still installed."
                : "Verifica a pasta storage\\t6 no disco agora para checar se os mods/mapas ativos ainda est\u00e3o instalados.");
            mainToolTip.SetToolTip(btnHelpGuide, en
                ? "Open step-by-step instructions on packs, custom maps, and how to load them in-game."
                : "Abre o guia passo a passo sobre perfis, mapas customizados e como ativ\u00e1-los no jogo.");
            mainToolTip.SetToolTip(btnAddFiles, en
                ? "Add .iwd, .iwi, .gsc, .csc, .ff, .zip, .7z, or .rar files to the selected profile."
                : "Adiciona arquivos .iwd, .iwi, .gsc, .csc, .ff, .zip, .7z ou .rar ao perfil selecionado.");
            mainToolTip.SetToolTip(btnAddFolder, en
                ? "Add a custom map folder (zm_ / mp_), camo texture folder, or scripts folder."
                : "Adiciona uma pasta de mapa customizado (zm_ / mp_), pasta de texturas ou pasta de scripts.");
            mainToolTip.SetToolTip(btnChangeDest, en
                ? "Override where the selected mod/map will be installed inside storage\\t6."
                : "Altera manualmente em qual subpasta de storage\\t6 o mod/mapa selecionado ser\u00e1 instalado.");
            mainToolTip.SetToolTip(btnActivateProfile, en
                ? "Clean previous managed mods and install all items from the selected profile into storage\\t6."
                : "Limpa os mods do perfil anterior e instala todos os itens do perfil selecionado em storage\\t6.");
            mainToolTip.SetToolTip(btnDeactivateAll, en
                ? "Remove all managed mods/maps from storage\\t6 and return to clean vanilla BO2."
                : "Remove todos os mods/mapas gerenciados de storage\\t6 e volta ao BO2 limpo (Vanilla).");
            mainToolTip.SetToolTip(btnExportExe, en
                ? "Package the selected profile into a single standalone .exe installer to share with friends."
                : "Empacota o perfil selecionado em um instalador .exe \u00fanico para compartilhar com amigos.");
            if (lnkGitHubFooter != null)
                mainToolTip.SetToolTip(lnkGitHubFooter, GitHubRepoUrl);
        }

        public MainForm()
        {
            InitStorage();
            currentLang = string.Equals(db.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "pt";
            InitUI();
            VerifyInstalledStateOnDisk(true);
            RefreshProfilesList();
            ApplyLanguage();
            UpdateActiveBanner();

            this.Activated += (s, e) =>
            {
                SyncDiskStateSilently();
            };

            this.Shown += (s, e) =>
            {
                StartAutomaticUpdateCheck(true);
            };
        }

        private void InitStorage()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            vaultRoot = Path.Combine(localApp, "Plutonium", "T6_ModManager_Vault");
            profilesVaultDir = Path.Combine(vaultRoot, "Profiles");
            dbFilePath = Path.Combine(vaultRoot, "database.json");
            activeManifestPath = Path.Combine(vaultRoot, "active_installed.manifest");

            Directory.CreateDirectory(vaultRoot);
            Directory.CreateDirectory(profilesVaultDir);

            LoadDatabase();

            if (string.IsNullOrWhiteSpace(db.T6Directory))
            {
                db.T6Directory = Path.Combine(localApp, "Plutonium", "storage", "t6");
            }

            // Migrate legacy Usermap (TargetTypeInt == 4) entries to CustomMapMod (mods\<map>) automatically
            bool migratedAny = false;
            for (int p = 0; p < db.Profiles.Count; p++)
            {
                ModProfile prof = db.Profiles[p];
                if (prof.Items == null) prof.Items = new List<ModEntryItem>();
                for (int i = 0; i < prof.Items.Count; i++)
                {
                    ModEntryItem item = prof.Items[i];
                    if (item.TargetType == ModTargetType.Usermap)
                    {
                        item.TargetType = ModTargetType.CustomMapMod;
                        if (!string.IsNullOrEmpty(item.PreviewPath) && item.PreviewPath.StartsWith("usermaps\\", StringComparison.OrdinalIgnoreCase))
                        {
                            item.PreviewPath = "mods\\" + item.PreviewPath.Substring("usermaps\\".Length);
                        }
                        migratedAny = true;
                    }
                }
            }
            if (migratedAny)
            {
                SaveDatabase();
            }

            if (db.Profiles.Count == 0)
            {
                ModProfile p1 = new ModProfile();
                p1.Id = Guid.NewGuid().ToString("N");
                p1.Name = "Pack Anime";
                p1.CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Profiles.Add(p1);

                ModProfile p2 = new ModProfile();
                p2.Id = Guid.NewGuid().ToString("N");
                p2.Name = "Zombies + Mapas Custom";
                p2.CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Profiles.Add(p2);

                SaveDatabase();
            }

            // Backfill camo/mode analysis for any existing items missing metadata
            bool needsBackfillSave = false;
            for (int p = 0; p < db.Profiles.Count; p++)
            {
                ModProfile prof = db.Profiles[p];
                if (prof.Items == null) continue;
                for (int i = 0; i < prof.Items.Count; i++)
                {
                    ModEntryItem it = prof.Items[i];
                    if (string.IsNullOrEmpty(it.DetectedMode) || string.IsNullOrEmpty(it.DetectedSummaryPt))
                    {
                        string itemDir = Path.Combine(profilesVaultDir, prof.Id, it.VaultSubFolder ?? "");
                        ModContentAnalyzer.AnalyzeVaultItem(itemDir, it);
                        needsBackfillSave = true;
                    }
                }
            }
            if (needsBackfillSave)
            {
                SaveDatabase();
            }
        }

        private void LoadDatabase()
        {
            if (File.Exists(dbFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(dbFilePath, FileMode.Open, FileAccess.Read))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ManagerDatabase));
                        db = (ManagerDatabase)ser.ReadObject(fs);
                    }
                }
                catch
                {
                    db = new ManagerDatabase();
                }
            }
            if (db == null) db = new ManagerDatabase();
            if (db.Profiles == null) db.Profiles = new List<ModProfile>();
        }

        private void SaveDatabase()
        {
            try
            {
                using (FileStream fs = new FileStream(dbFilePath, FileMode.Create, FileAccess.Write))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ManagerDatabase));
                    ser.WriteObject(fs, db);
                }
            }
            catch (Exception ex)
            {
                AppendLog("[ERRO] Falha ao salvar banco de dados: " + ex.Message);
            }
        }

        private string GetEffectiveT6Dir()
        {
            if (txtT6Path != null && !string.IsNullOrWhiteSpace(txtT6Path.Text))
                return txtT6Path.Text.Trim();
            return db != null && !string.IsNullOrWhiteSpace(db.T6Directory) ? db.T6Directory.Trim() : "";
        }

        private void SyncDiskStateSilently()
        {
            string prevActiveId = db.ActiveProfileId;
            DiskSyncState prevState = currentSyncState;
            int prevMissingCount = missingItemIds.Count;

            DiskSyncState newState = VerifyInstalledStateOnDisk(true);
            if (!string.Equals(prevActiveId, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase)
                || prevState != newState
                || prevMissingCount != missingItemIds.Count)
            {
                UpdateActiveBanner();
                RefreshSelectedProfileView();
                if (newState == DiskSyncState.AllMissing)
                {
                    lblStatus.ForeColor = AccentAmber;
                    lblStatus.Text = IsEn
                        ? "Active profile files were removed from storage\\t6. Status updated to Clean Game (Vanilla)."
                        : "Os arquivos do perfil ativo foram removidos de storage\\t6. Status atualizado para Jogo Limpo (Vanilla).";
                }
                else if (newState == DiskSyncState.PartiallyMissing)
                {
                    lblStatus.ForeColor = AccentCoral;
                    lblStatus.Text = IsEn
                        ? "Warning: " + missingItemIds.Count + " item(s) were deleted from storage\\t6. Click ACTIVATE PROFILE IN GAME to restore."
                        : "Aviso: " + missingItemIds.Count + " item(ns) foram removidos de storage\\t6. Clique em ATIVAR PERFIL NO JOGO para restaurar.";
                }
            }
        }

        private DiskSyncState VerifyInstalledStateOnDisk(bool logChanges)
        {
            missingItemIds.Clear();
            string t6Dir = GetEffectiveT6Dir();

            if (string.IsNullOrWhiteSpace(db.ActiveProfileId))
            {
                currentSyncState = DiskSyncState.Clean;
                return currentSyncState;
            }

            ModProfile activeProf = null;
            for (int i = 0; i < db.Profiles.Count; i++)
            {
                if (string.Equals(db.Profiles[i].Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
                {
                    activeProf = db.Profiles[i];
                    break;
                }
            }

            if (activeProf == null)
            {
                db.ActiveProfileId = "";
                SaveDatabase();
                currentSyncState = DiskSyncState.Clean;
                return currentSyncState;
            }

            if (string.IsNullOrEmpty(t6Dir) || !Directory.Exists(t6Dir))
            {
                if (File.Exists(activeManifestPath))
                {
                    try { File.Delete(activeManifestPath); } catch { }
                }
                db.ActiveProfileId = "";
                SaveDatabase();
                if (logChanges)
                {
                    AppendLog(IsEn
                        ? "[SYNC] T6 folder not found on disk. Active status cleared."
                        : "[SYNC] Pasta T6 n\u00e3o encontrada no disco. Status ativo limpo.");
                }
                currentSyncState = DiskSyncState.AllMissing;
                return currentSyncState;
            }

            if (File.Exists(activeManifestPath))
            {
                string[] lines = File.ReadAllLines(activeManifestPath, Encoding.UTF8);
                List<string> existingRelPaths = new List<string>();
                List<string> missingRelPaths = new List<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    string rel = lines[i].Trim();
                    if (string.IsNullOrEmpty(rel)) continue;
                    string full = Path.Combine(t6Dir, rel);
                    if (File.Exists(full))
                        existingRelPaths.Add(rel);
                    else
                        missingRelPaths.Add(rel);
                }

                if (lines.Length > 0 && existingRelPaths.Count == 0)
                {
                    try { File.Delete(activeManifestPath); } catch { }
                    db.ActiveProfileId = "";
                    SaveDatabase();
                    if (logChanges)
                    {
                        AppendLog(IsEn
                            ? "[SYNC] Installed files for '" + activeProf.Name + "' were removed from T6 folder. Reset to Vanilla."
                            : "[SYNC] Os arquivos instalados de '" + activeProf.Name + "' foram removidos da pasta T6. Status alterado para Jogo Limpo.");
                    }
                    currentSyncState = DiskSyncState.AllMissing;
                    return currentSyncState;
                }

                DetectMissingItemsForProfile(activeProf, t6Dir);

                if (missingRelPaths.Count > 0 || missingItemIds.Count > 0)
                {
                    try { File.WriteAllLines(activeManifestPath, existingRelPaths.ToArray(), Encoding.UTF8); } catch { }
                    if (logChanges)
                    {
                        AppendLog(IsEn
                            ? "[SYNC] Warning: " + missingRelPaths.Count + " file(s) from '" + activeProf.Name + "' are missing in T6 folder."
                            : "[SYNC] Aviso: " + missingRelPaths.Count + " arquivo(s) de '" + activeProf.Name + "' foram removidos da pasta T6.");
                    }
                    currentSyncState = DiskSyncState.PartiallyMissing;
                    return currentSyncState;
                }

                currentSyncState = DiskSyncState.SyncedOk;
                return currentSyncState;
            }
            else
            {
                int totalItems = activeProf.Items.Count;
                DetectMissingItemsForProfile(activeProf, t6Dir);

                if (totalItems > 0 && missingItemIds.Count >= totalItems)
                {
                    db.ActiveProfileId = "";
                    SaveDatabase();
                    if (logChanges)
                    {
                        AppendLog(IsEn
                            ? "[SYNC] Active profile '" + activeProf.Name + "' not found in T6 folder. Reset to Vanilla."
                            : "[SYNC] Perfil '" + activeProf.Name + "' n\u00e3o encontrado na pasta T6. Status alterado para Jogo Limpo.");
                    }
                    currentSyncState = DiskSyncState.AllMissing;
                    return currentSyncState;
                }
                else if (missingItemIds.Count > 0)
                {
                    currentSyncState = DiskSyncState.PartiallyMissing;
                    return currentSyncState;
                }

                currentSyncState = DiskSyncState.SyncedOk;
                return currentSyncState;
            }
        }

        private void DetectMissingItemsForProfile(ModProfile prof, string t6Dir)
        {
            if (prof == null || prof.Items == null) return;
            for (int i = 0; i < prof.Items.Count; i++)
            {
                ModEntryItem item = prof.Items[i];
                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                if (!Directory.Exists(itemVaultDir)) continue;

                string[] vaultFiles = Directory.GetFiles(itemVaultDir, "*", SearchOption.AllDirectories);
                if (vaultFiles.Length == 0) continue;

                string targetBaseDir = ResolveTargetBaseDir(t6Dir, prof, item, itemVaultDir);
                bool anyMissing = false;
                for (int f = 0; f < vaultFiles.Length; f++)
                {
                    string relToItem = GetRelativePath(itemVaultDir, vaultFiles[f]);
                    string expectedDest = Path.Combine(targetBaseDir, relToItem);
                    if (!File.Exists(expectedDest))
                    {
                        anyMissing = true;
                        break;
                    }
                }
                if (anyMissing)
                {
                    missingItemIds.Add(item.Id);
                }
            }
        }

        private string ResolveTargetBaseDir(string t6Dir, ModProfile prof, ModEntryItem item, string itemVaultDir)
        {
            switch (item.TargetType)
            {
                case ModTargetType.MainIwd:
                    return Path.Combine(t6Dir, "main");
                case ModTargetType.ScriptsZm:
                    return Path.Combine(t6Dir, "scripts", "zm");
                case ModTargetType.ScriptsMp:
                    return Path.Combine(t6Dir, "scripts", "mp");
                case ModTargetType.ScriptsRoot:
                    return Path.Combine(t6Dir, "scripts");
                case ModTargetType.Usermap:
                {
                    if (Directory.Exists(itemVaultDir)
                        && Directory.GetFiles(itemVaultDir).Length == 0
                        && Directory.GetDirectories(itemVaultDir).Length == 1)
                    {
                        return Path.Combine(t6Dir, "mods");
                    }
                    string legacyMapFolder = SanitizeFileName(StripMapPrefix(item.DisplayName));
                    return Path.Combine(t6Dir, "mods", legacyMapFolder);
                }
                case ModTargetType.CustomMapMod:
                {
                    if (Directory.Exists(itemVaultDir)
                        && Directory.GetFiles(itemVaultDir).Length == 0
                        && Directory.GetDirectories(itemVaultDir).Length == 1)
                    {
                        return Path.Combine(t6Dir, "mods");
                    }
                    string mapFolder = SanitizeFileName(StripMapPrefix(item.DisplayName));
                    return Path.Combine(t6Dir, "mods", mapFolder);
                }
                case ModTargetType.ModFastFile:
                {
                    if (Directory.Exists(itemVaultDir)
                        && Directory.GetFiles(itemVaultDir).Length == 0
                        && Directory.GetDirectories(itemVaultDir).Length == 1)
                    {
                        return Path.Combine(t6Dir, "mods");
                    }
                    string modFolder = SanitizeFileName(prof.Name);
                    return Path.Combine(t6Dir, "mods", modFolder);
                }
                default:
                    return Path.Combine(t6Dir, "main");
            }
        }

        private static string StripMapPrefix(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "CustomMap";
            string s = name.Trim();
            if (s.StartsWith("Mapa:", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(5).Trim();
            else if (s.StartsWith("Map:", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(4).Trim();
            return string.IsNullOrEmpty(s) ? "CustomMap" : s;
        }

        private void InitUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = L("win_title");
            this.Size = new Size(1080, 760);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgPrimary;
            this.ForeColor = TextLight;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            try
            {
                if (!string.IsNullOrEmpty(Application.ExecutablePath) && File.Exists(Application.ExecutablePath))
                {
                    Icon exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (exeIcon != null) this.Icon = exeIcon;
                }
            }
            catch { }

            mainToolTip = new ToolTip();
            mainToolTip.AutoPopDelay = 8000;
            mainToolTip.InitialDelay = 350;
            mainToolTip.ReshowDelay = 200;

            // 1. TOP HEADER
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 72;
            pnlHeader.BackColor = BgCard;

            Panel topLine = new Panel();
            topLine.Dock = DockStyle.Top;
            topLine.Height = 4;
            topLine.BackColor = AccentOrange;
            pnlHeader.Controls.Add(topLine);

            Label lblTitle = new Label();
            lblTitle.Text = "PLUTONIUM T6 // MOD & PACK MANAGER";
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = AccentOrange;
            lblTitle.Location = new Point(20, 12);
            lblTitle.AutoSize = true;

            Label lblSub = new Label();
            lblSubRef = lblSub;
            lblSub.Text = L("header_sub");
            lblSub.Font = new Font("Segoe UI", 8.8F, FontStyle.Regular);
            lblSub.ForeColor = TextMuted;
            lblSub.Location = new Point(22, 42);
            lblSub.AutoSize = true;

            btnCheckUpdate = CreateStyledButton(L("update_btn"), 636, 11, 122, 26, BgInput, AccentCyan);
            btnCheckUpdate.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            btnCheckUpdate.Click += BtnCheckUpdate_Click;

            btnCheckSync = CreateStyledButton(L("verify_t6"), 764, 11, 96, 26, BgInput, AccentGreen);
            btnCheckSync.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            btnCheckSync.Click += BtnCheckSync_Click;

            btnHelpGuide = CreateStyledButton(L("guide_btn"), 866, 11, 98, 26, BgInput, AccentCyan);
            btnHelpGuide.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            btnHelpGuide.Click += (s, e) => MessageBox.Show(L("guide_body"), L("guide_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);

            btnLangToggle = CreateStyledButton("[PT] / EN", 970, 11, 86, 26, BgInput, AccentOrange);
            btnLangToggle.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            btnLangToggle.Click += (s, e) => ToggleLanguage();

            lblActiveBanner = new Label();
            lblActiveBanner.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblActiveBanner.TextAlign = ContentAlignment.MiddleRight;
            lblActiveBanner.Location = new Point(620, 42);
            lblActiveBanner.Size = new Size(436, 20);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(btnCheckUpdate);
            pnlHeader.Controls.Add(btnCheckSync);
            pnlHeader.Controls.Add(btnHelpGuide);
            pnlHeader.Controls.Add(btnLangToggle);
            pnlHeader.Controls.Add(lblActiveBanner);
            this.Controls.Add(pnlHeader);

            // 2. PATH BAR
            Panel pnlPathBar = new Panel();
            pnlPathBar.Location = new Point(16, 82);
            pnlPathBar.Size = new Size(1040, 46);
            pnlPathBar.BackColor = BgCard;

            Label lblT6 = new Label();
            lblT6Ref = lblT6;
            lblT6.Text = L("t6_label");
            lblT6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblT6.ForeColor = TextMuted;
            lblT6.Location = new Point(14, 14);
            lblT6.AutoSize = true;
            pnlPathBar.Controls.Add(lblT6);

            txtT6Path = new TextBox();
            txtT6Path.Text = db.T6Directory;
            txtT6Path.Location = new Point(148, 11);
            txtT6Path.Size = new Size(542, 24);
            txtT6Path.BackColor = BgInput;
            txtT6Path.ForeColor = TextLight;
            txtT6Path.BorderStyle = BorderStyle.FixedSingle;
            txtT6Path.Leave += (s, e) =>
            {
                db.T6Directory = txtT6Path.Text.Trim();
                SaveDatabase();
                SyncDiskStateSilently();
            };
            pnlPathBar.Controls.Add(txtT6Path);

            btnBrowseT6 = CreateStyledButton(L("browse"), 698, 10, 86, 26, BgInput, TextLight);
            btnBrowseT6.Click += BtnBrowseT6_Click;
            pnlPathBar.Controls.Add(btnBrowseT6);

            btnOpenT6 = CreateStyledButton(L("open_t6"), 790, 10, 114, 26, BgInput, TextLight);
            btnOpenT6.Click += (s, e) =>
            {
                if (Directory.Exists(txtT6Path.Text))
                    Process.Start("explorer.exe", txtT6Path.Text);
                else
                    MessageBox.Show(
                        IsEn ? "The folder does not exist yet. It will be created when you activate a profile." : "A pasta ainda n\u00e3o existe. Ela ser\u00e1 criada ao ativar um perfil.",
                        IsEn ? "Notice" : "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
            };
            pnlPathBar.Controls.Add(btnOpenT6);

            btnLaunchPluto = CreateStyledButton(L("launch_pluto"), 910, 10, 116, 26, Color.FromArgb(42, 92, 62), TextLight);
            btnLaunchPluto.Font = new Font("Segoe UI", 8.8F, FontStyle.Bold);
            btnLaunchPluto.Click += BtnLaunchPluto_Click;
            pnlPathBar.Controls.Add(btnLaunchPluto);

            this.Controls.Add(pnlPathBar);

            // 3. INTUITIVE 3-STEP WORKFLOW STRIP
            Panel pnlStepsBar = new Panel();
            pnlStepsBar.Location = new Point(16, 136);
            pnlStepsBar.Size = new Size(1040, 34);
            pnlStepsBar.BackColor = Color.FromArgb(25, 27, 34);

            lblStep1 = new Label();
            lblStep1.Location = new Point(14, 8);
            lblStep1.Size = new Size(300, 18);
            lblStep1.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            lblStep1.ForeColor = TextMuted;

            lblStep2 = new Label();
            lblStep2.Location = new Point(324, 8);
            lblStep2.Size = new Size(360, 18);
            lblStep2.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            lblStep2.ForeColor = TextMuted;

            lblStep3 = new Label();
            lblStep3.Location = new Point(694, 8);
            lblStep3.Size = new Size(332, 18);
            lblStep3.Font = new Font("Segoe UI", 8.4F, FontStyle.Bold);
            lblStep3.ForeColor = TextMuted;

            pnlStepsBar.Controls.Add(lblStep1);
            pnlStepsBar.Controls.Add(lblStep2);
            pnlStepsBar.Controls.Add(lblStep3);
            this.Controls.Add(pnlStepsBar);

            // 4. LEFT PANEL (PROFILES)
            Panel pnlLeft = new Panel();
            pnlLeft.Location = new Point(16, 178);
            pnlLeft.Size = new Size(295, 456);
            pnlLeft.BackColor = BgCard;

            Label lblProfilesHdr = new Label();
            lblProfilesHdrRef = lblProfilesHdr;
            lblProfilesHdr.Text = L("profiles_hdr");
            lblProfilesHdr.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblProfilesHdr.ForeColor = AccentOrange;
            lblProfilesHdr.Location = new Point(14, 12);
            lblProfilesHdr.AutoSize = true;
            pnlLeft.Controls.Add(lblProfilesHdr);

            Label lblProfilesSub = new Label();
            lblProfilesSubRef = lblProfilesSub;
            lblProfilesSub.Text = L("profiles_sub");
            lblProfilesSub.Font = new Font("Segoe UI", 8.2F, FontStyle.Regular);
            lblProfilesSub.ForeColor = TextMuted;
            lblProfilesSub.Location = new Point(14, 32);
            lblProfilesSub.AutoSize = true;
            pnlLeft.Controls.Add(lblProfilesSub);

            lstProfiles = new ListBox();
            lstProfiles.Location = new Point(14, 56);
            lstProfiles.Size = new Size(267, 306);
            lstProfiles.BackColor = BgInput;
            lstProfiles.ForeColor = TextLight;
            lstProfiles.BorderStyle = BorderStyle.FixedSingle;
            lstProfiles.DrawMode = DrawMode.OwnerDrawFixed;
            lstProfiles.ItemHeight = 52;
            lstProfiles.DrawItem += LstProfiles_DrawItem;
            lstProfiles.SelectedIndexChanged += (s, e) => RefreshSelectedProfileView();
            pnlLeft.Controls.Add(lstProfiles);

            btnNewProfile = CreateStyledButton(L("new_profile"), 14, 372, 130, 32, AccentOrange, Color.White);
            btnNewProfile.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnNewProfile.FlatAppearance.BorderSize = 0;
            btnNewProfile.Click += BtnNewProfile_Click;
            pnlLeft.Controls.Add(btnNewProfile);

            btnRenameProfile = CreateStyledButton(L("rename_profile"), 151, 372, 130, 32, BgInput, TextLight);
            btnRenameProfile.Click += BtnRenameProfile_Click;
            pnlLeft.Controls.Add(btnRenameProfile);

            btnDuplicateProfile = CreateStyledButton(L("dup_profile"), 14, 410, 130, 32, BgInput, TextLight);
            btnDuplicateProfile.Click += BtnDuplicateProfile_Click;
            pnlLeft.Controls.Add(btnDuplicateProfile);

            btnDeleteProfile = CreateStyledButton(L("del_profile"), 151, 410, 130, 32, Color.FromArgb(95, 35, 42), TextLight);
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            pnlLeft.Controls.Add(btnDeleteProfile);

            this.Controls.Add(pnlLeft);

            // 5. RIGHT PANEL (MODS IN SELECTED PROFILE)
            Panel pnlRight = new Panel();
            pnlRight.Location = new Point(321, 178);
            pnlRight.Size = new Size(735, 456);
            pnlRight.BackColor = BgCard;

            lblSelectedProfileTitle = new Label();
            lblSelectedProfileTitle.Text = "Pack Anime";
            lblSelectedProfileTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblSelectedProfileTitle.ForeColor = TextLight;
            lblSelectedProfileTitle.Location = new Point(16, 10);
            lblSelectedProfileTitle.Size = new Size(520, 24);
            pnlRight.Controls.Add(lblSelectedProfileTitle);

            lblSelectedProfileStats = new Label();
            lblSelectedProfileStats.Text = "0 mod(s) | main: 0 | scripts: 0 | mods: 0";
            lblSelectedProfileStats.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblSelectedProfileStats.ForeColor = TextMuted;
            lblSelectedProfileStats.Location = new Point(17, 35);
            lblSelectedProfileStats.Size = new Size(540, 18);
            pnlRight.Controls.Add(lblSelectedProfileStats);

            btnAddFiles = CreateStyledButton(L("add_files"), 16, 60, 118, 32, AccentOrange, Color.White);
            btnAddFiles.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAddFiles.FlatAppearance.BorderSize = 0;
            btnAddFiles.Click += BtnAddFiles_Click;
            pnlRight.Controls.Add(btnAddFiles);

            btnAddFolder = CreateStyledButton(L("add_folder"), 140, 60, 174, 32, BgInput, TextLight);
            btnAddFolder.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAddFolder.Click += BtnAddFolder_Click;
            pnlRight.Controls.Add(btnAddFolder);

            btnChangeDest = CreateStyledButton(L("change_dest"), 320, 60, 132, 32, BgInput, AccentCyan);
            btnChangeDest.Click += BtnChangeDest_Click;
            pnlRight.Controls.Add(btnChangeDest);

            btnRemoveItem = CreateStyledButton(L("remove_item"), 566, 60, 153, 32, Color.FromArgb(95, 35, 42), TextLight);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            pnlRight.Controls.Add(btnRemoveItem);

            ctxDestMenu = new ContextMenuStrip();
            ctxDestMenu.Items.Add("main", null, (s, e) => ChangeSelectedModDestination(ModTargetType.MainIwd));
            ctxDestMenu.Items.Add("scripts\\zm", null, (s, e) => ChangeSelectedModDestination(ModTargetType.ScriptsZm));
            ctxDestMenu.Items.Add("scripts\\mp", null, (s, e) => ChangeSelectedModDestination(ModTargetType.ScriptsMp));
            ctxDestMenu.Items.Add("scripts", null, (s, e) => ChangeSelectedModDestination(ModTargetType.ScriptsRoot));
            ctxDestMenu.Items.Add("mods\\<mapa>", null, (s, e) => ChangeSelectedModDestination(ModTargetType.CustomMapMod));
            ctxDestMenu.Items.Add("mods\\<mod>", null, (s, e) => ChangeSelectedModDestination(ModTargetType.ModFastFile));

            // Contextual Custom Map Notice Strip (visible when selected profile has custom maps)
            pnlMapNotice = new Panel();
            pnlMapNotice.Location = new Point(16, 98);
            pnlMapNotice.Size = new Size(703, 34);
            pnlMapNotice.BackColor = Color.FromArgb(46, 33, 66);
            pnlMapNotice.Visible = false;

            Panel mapNoticeLeftBar = new Panel();
            mapNoticeLeftBar.Dock = DockStyle.Left;
            mapNoticeLeftBar.Width = 4;
            mapNoticeLeftBar.BackColor = AccentPurple;
            pnlMapNotice.Controls.Add(mapNoticeLeftBar);

            lblMapNotice = new Label();
            lblMapNotice.Dock = DockStyle.Fill;
            lblMapNotice.Padding = new Padding(10, 0, 8, 0);
            lblMapNotice.TextAlign = ContentAlignment.MiddleLeft;
            lblMapNotice.Font = new Font("Segoe UI", 8.2F, FontStyle.Bold);
            lblMapNotice.ForeColor = Color.FromArgb(230, 205, 255);
            lblMapNotice.Text = L("map_notice_inline");
            pnlMapNotice.Controls.Add(lblMapNotice);
            pnlRight.Controls.Add(pnlMapNotice);

            lstModItems = new ListBox();
            lstModItems.Location = new Point(16, 98);
            lstModItems.Size = new Size(703, 226);
            lstModItems.BackColor = BgInput;
            lstModItems.ForeColor = TextLight;
            lstModItems.BorderStyle = BorderStyle.FixedSingle;
            lstModItems.DrawMode = DrawMode.OwnerDrawFixed;
            lstModItems.ItemHeight = 52;
            lstModItems.SelectionMode = SelectionMode.MultiExtended;
            lstModItems.AllowDrop = true;
            lstModItems.DrawItem += LstModItems_DrawItem;
            lstModItems.Paint += LstModItems_Paint;
            lstModItems.DragEnter += LstModItems_DragEnter;
            lstModItems.DragDrop += LstModItems_DragDrop;
            lstModItems.DoubleClick += LstModItems_DoubleClick;
            lstModItems.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && lstModItems.SelectedIndices.Count > 0)
                {
                    BtnRemoveItem_Click(s, EventArgs.Empty);
                }
                else if (e.KeyCode == Keys.F5)
                {
                    BtnCheckSync_Click(s, EventArgs.Empty);
                }
            };
            lstModItems.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && lstModItems.SelectedIndices.Count > 0)
                    ctxDestMenu.Show(lstModItems, e.Location);
            };
            pnlRight.Controls.Add(lstModItems);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(16, 332);
            progressBar.Size = new Size(703, 14);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            pnlRight.Controls.Add(progressBar);

            lblStatus = new Label();
            lblStatus.Text = L("default_hint");
            lblStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblStatus.ForeColor = TextMuted;
            lblStatus.Location = new Point(16, 351);
            lblStatus.Size = new Size(703, 18);
            pnlRight.Controls.Add(lblStatus);

            rtbLog = new RichTextBox();
            rtbLog.Location = new Point(16, 373);
            rtbLog.Size = new Size(703, 72);
            rtbLog.BackColor = Color.FromArgb(14, 15, 18);
            rtbLog.ForeColor = Color.FromArgb(180, 190, 205);
            rtbLog.BorderStyle = BorderStyle.FixedSingle;
            rtbLog.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
            rtbLog.ReadOnly = true;
            pnlRight.Controls.Add(rtbLog);

            this.Controls.Add(pnlRight);

            // 6. BOTTOM ACTION BAR
            Panel pnlBottom = new Panel();
            pnlBottom.Location = new Point(16, 644);
            pnlBottom.Size = new Size(1040, 50);
            pnlBottom.BackColor = BgCard;

            btnActivateProfile = CreateStyledButton(L("activate"), 14, 7, 275, 36, AccentGreen, Color.FromArgb(15, 23, 18));
            btnActivateProfile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnActivateProfile.FlatAppearance.BorderSize = 0;
            btnActivateProfile.Click += BtnActivateProfile_Click;
            pnlBottom.Controls.Add(btnActivateProfile);

            btnDeactivateAll = CreateStyledButton(L("deactivate"), 299, 7, 225, 36, BgInput, TextLight);
            btnDeactivateAll.Click += BtnDeactivateAll_Click;
            pnlBottom.Controls.Add(btnDeactivateAll);

            btnExportExe = CreateStyledButton(L("export_exe"), 766, 7, 260, 36, AccentOrange, Color.White);
            btnExportExe.Font = new Font("Segoe UI", 9.2F, FontStyle.Bold);
            btnExportExe.FlatAppearance.BorderSize = 0;
            btnExportExe.Click += BtnExportExe_Click;
            pnlBottom.Controls.Add(btnExportExe);

            this.Controls.Add(pnlBottom);

            // 7. FOOTER CREDITS & GITHUB LINK
            lblCreditFooter = new Label();
            lblCreditFooter.Text = L("credit_footer");
            lblCreditFooter.Font = new Font("Segoe UI", 8.2F, FontStyle.Regular);
            lblCreditFooter.ForeColor = TextMuted;
            lblCreditFooter.Location = new Point(18, 700);
            lblCreditFooter.AutoSize = true;
            this.Controls.Add(lblCreditFooter);

            lnkGitHubFooter = new LinkLabel();
            lnkGitHubFooter.Text = L("github_link");
            lnkGitHubFooter.Font = new Font("Segoe UI", 8.3F, FontStyle.Bold);
            lnkGitHubFooter.LinkColor = AccentOrange;
            lnkGitHubFooter.ActiveLinkColor = Color.White;
            lnkGitHubFooter.VisitedLinkColor = AccentOrange;
            lnkGitHubFooter.Location = new Point(820, 700);
            lnkGitHubFooter.Size = new Size(236, 18);
            lnkGitHubFooter.TextAlign = ContentAlignment.MiddleRight;
            lnkGitHubFooter.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(GitHubRepoUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, GitHubRepoUrl + "\n\n" + ex.Message, "GitHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            this.Controls.Add(lnkGitHubFooter);

            AppendLog(IsEn ? "Manager started. Vault: " + vaultRoot : "Gerenciador iniciado. Vault: " + vaultRoot);
        }

        private Button CreateStyledButton(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = BorderSubtle;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void AppendLog(string text)
        {
            if (rtbLog == null) return;
            rtbLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + "\r\n");
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
            Application.DoEvents();
        }

        private ModProfile GetSelectedProfile()
        {
            int idx = lstProfiles.SelectedIndex;
            if (idx >= 0 && idx < db.Profiles.Count)
                return db.Profiles[idx];
            return null;
        }

        private void RefreshProfilesList()
        {
            int prevIdx = lstProfiles.SelectedIndex;
            lstProfiles.Items.Clear();
            for (int i = 0; i < db.Profiles.Count; i++)
            {
                lstProfiles.Items.Add(db.Profiles[i]);
            }
            if (db.Profiles.Count > 0)
            {
                if (prevIdx >= 0 && prevIdx < db.Profiles.Count)
                    lstProfiles.SelectedIndex = prevIdx;
                else
                    lstProfiles.SelectedIndex = 0;
            }
            else
            {
                RefreshSelectedProfileView();
            }
        }

        private void UpdateWorkflowSteps(ModProfile prof, bool isActiveProfile, bool hasCustomMap)
        {
            if (lblStep1 == null || lblStep2 == null || lblStep3 == null) return;
            bool en = IsEn;

            if (prof == null)
            {
                lblStep1.Text = en ? "1. Create or pick a Profile" : "1. Crie ou escolha um Perfil";
                lblStep1.ForeColor = AccentOrange;
                lblStep2.Text = en ? "2. Add mods or custom maps" : "2. Adicione mods ou mapas custom";
                lblStep2.ForeColor = TextMuted;
                lblStep3.Text = en ? "3. Click ACTIVATE PROFILE IN GAME" : "3. Clique em ATIVAR PERFIL NO JOGO";
                lblStep3.ForeColor = TextMuted;
                return;
            }

            lblStep1.Text = en ? "1. Profile: " + prof.Name : "1. Perfil: " + prof.Name;
            lblStep1.ForeColor = AccentGreen;

            if (prof.Items.Count == 0)
            {
                lblStep2.Text = en ? "2. Add mods/maps here (drag & drop)" : "2. Adicione mods/mapas aqui (arraste)";
                lblStep2.ForeColor = AccentOrange;
                lblStep3.Text = en ? "3. Then activate in game" : "3. Depois ative no jogo";
                lblStep3.ForeColor = TextMuted;
            }
            else if (!isActiveProfile || currentSyncState == DiskSyncState.PartiallyMissing)
            {
                lblStep2.Text = en ? "2. " + prof.Items.Count + " item(s) ready in Vault" : "2. " + prof.Items.Count + " item(ns) prontos no Vault";
                lblStep2.ForeColor = AccentGreen;
                lblStep3.Text = en ? "3. Click ACTIVATE PROFILE IN GAME ->" : "3. Clique em ATIVAR PERFIL NO JOGO ->";
                lblStep3.ForeColor = AccentOrange;
            }
            else
            {
                lblStep2.Text = en ? "2. " + prof.Items.Count + " item(s) installed in T6" : "2. " + prof.Items.Count + " item(ns) instalados em T6";
                lblStep2.ForeColor = AccentGreen;
                if (hasCustomMap)
                {
                    lblStep3.Text = en ? "3. In-game: open Mods menu (ZM/MP)!" : "3. No jogo: abra o menu Mods (ZM/MP)!";
                    lblStep3.ForeColor = AccentPurple;
                }
                else
                {
                    lblStep3.Text = en ? "3. Active! Launch Plutonium BO2" : "3. Ativo! Abra o Plutonium BO2";
                    lblStep3.ForeColor = AccentGreen;
                }
            }
        }

        private void RefreshSelectedProfileView()
        {
            lstModItems.Items.Clear();
            ModProfile prof = GetSelectedProfile();
            if (prof == null)
            {
                lblSelectedProfileTitle.Text = IsEn ? "No profile selected" : "Nenhum perfil selecionado";
                lblSelectedProfileStats.Text = "";
                pnlMapNotice.Visible = false;
                lstModItems.Location = new Point(16, 98);
                lstModItems.Size = new Size(703, 226);
                UpdateWorkflowSteps(null, false, false);
                lstModItems.Invalidate();
                return;
            }

            bool isActive = string.Equals(prof.Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase);
            string activeTag;
            if (isActive && currentSyncState == DiskSyncState.PartiallyMissing)
            {
                activeTag = IsEn ? "  [ACTIVE - MISSING FILES IN T6]" : "  [ATIVO - ARQUIVOS FALTANDO EM T6]";
                lblSelectedProfileTitle.ForeColor = AccentCoral;
            }
            else if (isActive)
            {
                activeTag = IsEn ? "  [ACTIVE IN GAME]" : "  [ATIVO NO JOGO]";
                lblSelectedProfileTitle.ForeColor = AccentGreen;
            }
            else
            {
                activeTag = "";
                lblSelectedProfileTitle.ForeColor = TextLight;
            }
            lblSelectedProfileTitle.Text = prof.Name + activeTag;

            int countMain = 0;
            int countScripts = 0;
            int countModsAndMaps = 0;
            bool hasCustomMap = false;
            long totalBytes = 0;

            for (int i = 0; i < prof.Items.Count; i++)
            {
                ModEntryItem item = prof.Items[i];
                lstModItems.Items.Add(item);
                totalBytes += item.SizeBytes;
                if (item.TargetType == ModTargetType.MainIwd)
                    countMain++;
                else if (item.TargetType == ModTargetType.CustomMapMod || item.TargetType == ModTargetType.Usermap)
                {
                    countModsAndMaps++;
                    hasCustomMap = true;
                }
                else if (item.TargetType == ModTargetType.ModFastFile)
                {
                    countModsAndMaps++;
                }
                else if (item.TargetType == ModTargetType.ScriptsZm || item.TargetType == ModTargetType.ScriptsMp || item.TargetType == ModTargetType.ScriptsRoot)
                {
                    countScripts++;
                }
            }

            lblSelectedProfileStats.Text = string.Format(
                "{0} mod(s) ({1})  |  main: {2}  |  scripts: {3}  |  mods/mapas: {4}",
                prof.Items.Count,
                FormatSize(totalBytes),
                countMain,
                countScripts,
                countModsAndMaps
            );

            pnlMapNotice.Visible = hasCustomMap;
            if (hasCustomMap)
            {
                lblMapNotice.Text = L("map_notice_inline");
                lstModItems.Location = new Point(16, 138);
                lstModItems.Size = new Size(703, 186);
            }
            else
            {
                lstModItems.Location = new Point(16, 98);
                lstModItems.Size = new Size(703, 226);
            }

            UpdateWorkflowSteps(prof, isActive, hasCustomMap);
            lstProfiles.Invalidate();
            lstModItems.Invalidate();
        }

        private void UpdateActiveBanner()
        {
            ModProfile active = null;
            for (int i = 0; i < db.Profiles.Count; i++)
            {
                if (string.Equals(db.Profiles[i].Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
                {
                    active = db.Profiles[i];
                    break;
                }
            }

            if (active != null)
            {
                if (currentSyncState == DiskSyncState.PartiallyMissing)
                {
                    lblActiveBanner.ForeColor = AccentCoral;
                    lblActiveBanner.Text = L("status_partial") + active.Name.ToUpperInvariant();
                }
                else
                {
                    lblActiveBanner.ForeColor = AccentGreen;
                    lblActiveBanner.Text = L("status_active") + active.Name.ToUpperInvariant();
                }
            }
            else
            {
                lblActiveBanner.ForeColor = TextMuted;
                lblActiveBanner.Text = L("status_vanilla");
            }
            lstProfiles.Invalidate();
        }

        private void LstProfiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= db.Profiles.Count) return;
            ModProfile prof = db.Profiles[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isActive = string.Equals(prof.Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase);

            Color rowBg = selected ? Color.FromArgb(48, 53, 68) : BgInput;
            using (SolidBrush b = new SolidBrush(rowBg))
                e.Graphics.FillRectangle(b, e.Bounds);

            if (selected)
            {
                using (SolidBrush bar = new SolidBrush(AccentOrange))
                    e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);
            }

            long totalSize = 0;
            bool hasMap = false;
            for (int i = 0; i < prof.Items.Count; i++)
            {
                totalSize += prof.Items[i].SizeBytes;
                if (prof.Items[i].TargetType == ModTargetType.CustomMapMod || prof.Items[i].TargetType == ModTargetType.Usermap)
                    hasMap = true;
            }

            Color titleColor = TextLight;
            string pTag = "";
            if (isActive && currentSyncState == DiskSyncState.PartiallyMissing)
            {
                titleColor = AccentCoral;
                pTag = IsEn ? "  [PARTIAL]" : "  [PARCIAL]";
            }
            else if (isActive)
            {
                titleColor = AccentGreen;
                pTag = IsEn ? "  [ACTIVE]" : "  [ATIVO]";
            }

            using (Font fTitle = new Font("Segoe UI", 9.6F, FontStyle.Bold))
            using (SolidBrush bText = new SolidBrush(titleColor))
            {
                string title = prof.Name + pTag;
                e.Graphics.DrawString(title, fTitle, bText, e.Bounds.X + 10, e.Bounds.Y + 7);
            }

            using (Font fSub = new Font("Segoe UI", 8.1F, FontStyle.Regular))
            using (SolidBrush bMuted = new SolidBrush(TextMuted))
            {
                string mapNote = hasMap ? (IsEn ? "  |  Custom Map" : "  |  Mapa Custom") : "";
                string sub = prof.Items.Count + " mod(s)  |  " + FormatSize(totalSize) + mapNote;
                e.Graphics.DrawString(sub, fSub, bMuted, e.Bounds.X + 10, e.Bounds.Y + 29);
            }

            using (Pen p = new Pen(Color.FromArgb(30, 32, 40)))
                e.Graphics.DrawLine(p, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void LstModItems_Paint(object sender, PaintEventArgs e)
        {
            if (lstModItems.Items.Count > 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(18, 16, lstModItems.Width - 36, lstModItems.Height - 32);
            using (Pen dashPen = new Pen(Color.FromArgb(72, 78, 96), 1.5f))
            {
                dashPen.DashStyle = DashStyle.Dash;
                e.Graphics.DrawRectangle(dashPen, box);
            }

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            string mainTitle = IsEn
                ? "Drag & drop mod files or custom map folders here"
                : "Arraste arquivos de mods ou pastas de mapas customizados aqui";
            string subTitle = IsEn
                ? "Supported: .iwd, .iwi, .gsc, .csc, .ff, .zip, .7z, .rar or custom ZM/MP map folders\nOr use '+ Files' / '+ Mod / Map Folder' above."
                : "Suporta: .iwd, .iwi, .gsc, .csc, .ff, .zip, .7z, .rar ou pastas de mapas ZM/MP\nOu clique em '+ Arquivos' / '+ Pasta de Mod / Mapa' acima.";

            Rectangle rTop = new Rectangle(box.X + 12, box.Y + 18, box.Width - 24, 32);
            Rectangle rBot = new Rectangle(box.X + 12, box.Y + 52, box.Width - 24, 48);

            using (Font fMain = new Font("Segoe UI", 10.5F, FontStyle.Bold))
            using (SolidBrush bMain = new SolidBrush(TextLight))
            {
                e.Graphics.DrawString(mainTitle, fMain, bMain, rTop, sf);
            }

            using (Font fSub = new Font("Segoe UI", 8.6F, FontStyle.Regular))
            using (SolidBrush bSub = new SolidBrush(TextMuted))
            {
                e.Graphics.DrawString(subTitle, fSub, bSub, rBot, sf);
            }
        }

        private void LstModItems_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstModItems.Items.Count) return;
            ModEntryItem item = (ModEntryItem)lstModItems.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            ModProfile prof = GetSelectedProfile();
            bool profIsActive = prof != null && string.Equals(prof.Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase);
            bool isMissingOnDisk = profIsActive && missingItemIds.Contains(item.Id);

            Color rowBg = selected ? Color.FromArgb(48, 53, 68) : BgInput;
            using (SolidBrush b = new SolidBrush(rowBg))
                e.Graphics.FillRectangle(b, e.Bounds);

            string badgeText;
            Color badgeColor;
            GetBadgeInfo(item.TargetType, out badgeText, out badgeColor);

            Rectangle badgeRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 14, 114, 24);
            using (SolidBrush bBadge = new SolidBrush(badgeColor))
                e.Graphics.FillRectangle(bBadge, badgeRect);

            using (Font fBadge = new Font("Segoe UI", 7.8F, FontStyle.Bold))
            using (SolidBrush bDark = new SolidBrush(Color.FromArgb(18, 20, 24)))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(badgeText, fBadge, bDark, badgeRect, sf);
            }

            int cursorX = e.Bounds.X + 134;
            int topY = e.Bounds.Y + 5;

            string diskTag = "";
            Color nameColor = TextLight;
            if (isMissingOnDisk)
            {
                diskTag = IsEn ? "  [MISSING IN T6]" : "  [REMOVIDO DE T6]";
                nameColor = AccentCoral;
            }
            else if (profIsActive)
            {
                diskTag = IsEn ? "  [INSTALLED]" : "  [INSTALADO]";
            }

            string titleText = item.DisplayName + diskTag;
            using (Font fName = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (SolidBrush bText = new SolidBrush(nameColor))
            {
                SizeF nameSize = e.Graphics.MeasureString(titleText, fName);
                int maxNameW = Math.Max(130, e.Bounds.Width - 390);
                RectangleF nameRect = new RectangleF(cursorX, topY, Math.Min(nameSize.Width + 4, maxNameW), 20);
                e.Graphics.DrawString(titleText, fName, bText, nameRect);
                cursorX += (int)nameRect.Width + 6;
            }

            // Mode Pill ([ZM] / [MP] / [MP/ZM]) right next to the title
            string modeTag = string.IsNullOrEmpty(item.DetectedMode) ? "MP/ZM" : item.DetectedMode;
            Color modeBg = (modeTag == "ZM") ? AccentPurple : (modeTag == "MP" ? Color.FromArgb(14, 116, 144) : Color.FromArgb(146, 64, 14));
            int modePillW = (modeTag == "MP/ZM") ? 46 : 34;
            Rectangle modeRect = new Rectangle(cursorX, topY + 1, modePillW, 17);
            using (SolidBrush bModeBg = new SolidBrush(modeBg))
                e.Graphics.FillRectangle(bModeBg, modeRect);

            using (Font fMode = new Font("Segoe UI", 7.2F, FontStyle.Bold))
            using (SolidBrush bModeFg = new SolidBrush(Color.White))
            using (StringFormat sfMode = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                e.Graphics.DrawString(modeTag, fMode, bModeFg, modeRect, sfMode);
            }
            cursorX += modePillW + 6;

            // Specific Camouflage / Replaced Item right next to the mode pill
            string detectedDesc = IsEn
                ? (string.IsNullOrEmpty(item.DetectedSummaryEn) ? "" : item.DetectedSummaryEn)
                : (string.IsNullOrEmpty(item.DetectedSummaryPt) ? "" : item.DetectedSummaryPt);

            if (!string.IsNullOrEmpty(detectedDesc))
            {
                int remW = e.Bounds.Right - cursorX - 10;
                if (remW > 45)
                {
                    using (Font fDet = new Font("Segoe UI", 8.4F, FontStyle.Bold))
                    using (SolidBrush bDet = new SolidBrush(AccentCyan))
                    {
                        RectangleF detRect = new RectangleF(cursorX, topY + 1, remW, 19);
                        e.Graphics.DrawString("\u2022 " + detectedDesc, fDet, bDet, detRect);
                    }
                }
            }

            using (Font fSub = new Font("Segoe UI", 8.2F, FontStyle.Regular))
            using (SolidBrush bMuted = new SolidBrush(TextMuted))
            {
                bool isEn = IsEn;
                string destLbl = isEn ? "Target: storage\\t6\\" : "Destino: storage\\t6\\";
                string fileLbl = isEn ? "files" : "arq.";
                string extraHint = (item.TargetType == ModTargetType.CustomMapMod || item.TargetType == ModTargetType.ModFastFile)
                    ? (isEn ? "  [Load in Mods menu]" : "  [Ativar no menu Mods]")
                    : "";
                string destInfo = destLbl + item.PreviewPath + extraHint + "   (" + item.FileCount + " " + fileLbl + " | " + FormatSize(item.SizeBytes) + ")";
                e.Graphics.DrawString(destInfo, fSub, bMuted, e.Bounds.X + 134, e.Bounds.Y + 27);
            }

            using (Pen p = new Pen(Color.FromArgb(30, 32, 40)))
                e.Graphics.DrawLine(p, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void LstModItems_DoubleClick(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null || lstModItems.SelectedIndex < 0) return;

            ModEntryItem item = prof.Items[lstModItems.SelectedIndex];
            string dir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
            if (!Directory.Exists(dir)) return;

            string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Mod: " + item.DisplayName);
            sb.AppendLine((IsEn ? "Mode: " : "Modo: ") + "[" + (item.DetectedMode ?? "MP/ZM") + "]");
            sb.AppendLine((IsEn ? "Detected Replacement: " : "Item / Camuflagem Detectada: ") +
                (IsEn ? (item.DetectedSummaryEn ?? "-") : (item.DetectedSummaryPt ?? "-")));
            if (!string.IsNullOrEmpty(item.DetectedItemsList))
                sb.AppendLine((IsEn ? "All Matched Textures: " : "Texturas Identificadas: ") + item.DetectedItemsList);
            sb.AppendLine((IsEn ? "Target: storage\\t6\\" : "Destino: storage\\t6\\") + item.PreviewPath);
            sb.AppendLine((IsEn ? "Size: " : "Tamanho: ") + FormatSize(item.SizeBytes) + " (" + files.Length + (IsEn ? " file(s))" : " arquivo(s))"));
            if (item.TargetType == ModTargetType.CustomMapMod)
            {
                sb.AppendLine(IsEn
                    ? "How to play: Open 'Mods' in the Zombies or Multiplayer main menu and load this map."
                    : "Como jogar: Abra 'Mods' no menu principal do Zombies ou Multiplayer e carregue este mapa.");
            }
            sb.AppendLine("----------------------------------------");
            int limit = Math.Min(files.Length, 25);
            for (int i = 0; i < limit; i++)
                sb.AppendLine(" - " + GetRelativePath(dir, files[i]));
            if (files.Length > 25)
                sb.AppendLine(" ... +" + (files.Length - 25) + (IsEn ? " more file(s)." : " arquivo(s)."));

            MessageBox.Show(sb.ToString(), IsEn ? "Mod Details" : "Detalhes do Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetBadgeInfo(ModTargetType type, out string label, out Color color)
        {
            switch (type)
            {
                case ModTargetType.MainIwd:
                    label = "MAIN / IWD";
                    color = AccentOrange;
                    break;
                case ModTargetType.ScriptsZm:
                    label = "SCRIPTS / ZM";
                    color = AccentCyan;
                    break;
                case ModTargetType.ScriptsMp:
                    label = "SCRIPTS / MP";
                    color = AccentCyan;
                    break;
                case ModTargetType.ScriptsRoot:
                    label = "SCRIPTS";
                    color = AccentCyan;
                    break;
                case ModTargetType.CustomMapMod:
                    label = IsEn ? "MAP (MODS)" : "MAPA (MODS)";
                    color = AccentPurple;
                    break;
                case ModTargetType.Usermap:
                    label = "USERMAPS";
                    color = AccentPurple;
                    break;
                case ModTargetType.ModFastFile:
                    label = "MODS / FF";
                    color = AccentCoral;
                    break;
                default:
                    label = "MOD";
                    color = TextMuted;
                    break;
            }
        }

        private void BtnCheckSync_Click(object sender, EventArgs e)
        {
            DiskSyncState st = VerifyInstalledStateOnDisk(true);
            UpdateActiveBanner();
            RefreshSelectedProfileView();

            if (st == DiskSyncState.SyncedOk)
            {
                lblStatus.ForeColor = AccentGreen;
                lblStatus.Text = IsEn
                    ? "Verified: All active profile files are properly installed in storage\\t6."
                    : "Verificado: Todos os arquivos do perfil ativo est\u00e3o instalados em storage\\t6.";
            }
            else if (st == DiskSyncState.Clean)
            {
                lblStatus.ForeColor = TextMuted;
                lblStatus.Text = IsEn
                    ? "Verified: No active profile installed (Clean Game / Vanilla)."
                    : "Verificado: Nenhum perfil ativo instalado (Jogo Limpo / Vanilla).";
            }
            else if (st == DiskSyncState.AllMissing)
            {
                lblStatus.ForeColor = AccentAmber;
                lblStatus.Text = IsEn
                    ? "Verified: Previous active files were removed from storage\\t6. Status reset to Vanilla."
                    : "Verificado: Os arquivos anteriores foram removidos de storage\\t6. Status atualizado para Jogo Limpo.";
            }
            else if (st == DiskSyncState.PartiallyMissing)
            {
                lblStatus.ForeColor = AccentCoral;
                lblStatus.Text = IsEn
                    ? "Verified: " + missingItemIds.Count + " item(s) missing from storage\\t6. Click ACTIVATE PROFILE IN GAME to restore."
                    : "Verificado: " + missingItemIds.Count + " item(ns) faltando em storage\\t6. Clique em ATIVAR PERFIL NO JOGO para restaurar.";
            }
        }

        private void BtnBrowseT6_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = IsEn ? "Select Plutonium storage\\t6 folder" : "Selecione a pasta storage\\t6 do Plutonium";
                if (Directory.Exists(txtT6Path.Text))
                    fbd.SelectedPath = txtT6Path.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtT6Path.Text = fbd.SelectedPath;
                    db.T6Directory = fbd.SelectedPath;
                    SaveDatabase();
                    SyncDiskStateSilently();
                }
            }
        }

        private void BtnLaunchPluto_Click(object sender, EventArgs e)
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates = new string[]
            {
                Path.Combine(localApp, "Plutonium", "bin", "plutonium-launcher-win32.exe"),
                Path.Combine(localApp, "Plutonium", "plutonium.exe")
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    Process.Start(candidates[i]);
                    return;
                }
            }
            MessageBox.Show(
                IsEn ? "Plutonium executable was not found automatically in AppData\\Local\\Plutonium." : "Execut\u00e1vel do Plutonium n\u00e3o encontrado automaticamente em AppData\\Local\\Plutonium.",
                "Plutonium",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void BtnNewProfile_Click(object sender, EventArgs e)
        {
            string name = PromptInput(
                IsEn ? "New Mod Pack" : "Novo Perfil de Mods",
                IsEn ? "Enter the name for the new pack (e.g. Anime Pack, Custom Zombies):" : "Digite o nome da nova lista (ex: Pack Anime, Zombies Cl\u00e1ssico):",
                IsEn ? "New Pack" : "Novo Pack"
            );
            if (string.IsNullOrWhiteSpace(name)) return;

            ModProfile p = new ModProfile();
            p.Id = Guid.NewGuid().ToString("N");
            p.Name = name.Trim();
            p.CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            db.Profiles.Add(p);
            SaveDatabase();

            RefreshProfilesList();
            lstProfiles.SelectedIndex = db.Profiles.Count - 1;
            AppendLog((IsEn ? "Profile created: " : "Perfil criado: ") + p.Name);
        }

        private void BtnRenameProfile_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            string name = PromptInput(
                IsEn ? "Rename Profile" : "Renomear Perfil",
                IsEn ? "New name for this profile:" : "Novo nome para o perfil:",
                prof.Name
            );
            if (string.IsNullOrWhiteSpace(name)) return;

            prof.Name = name.Trim();
            SaveDatabase();
            RefreshProfilesList();
            UpdateActiveBanner();
        }

        private void BtnDuplicateProfile_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            ModProfile copy = new ModProfile();
            copy.Id = Guid.NewGuid().ToString("N");
            copy.Name = prof.Name + (IsEn ? " (Copy)" : " (C\u00f3pia)");
            copy.CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            string srcProfDir = Path.Combine(profilesVaultDir, prof.Id);
            string dstProfDir = Path.Combine(profilesVaultDir, copy.Id);
            Directory.CreateDirectory(dstProfDir);

            for (int i = 0; i < prof.Items.Count; i++)
            {
                ModEntryItem orig = prof.Items[i];
                ModEntryItem clone = new ModEntryItem();
                clone.Id = Guid.NewGuid().ToString("N");
                clone.DisplayName = orig.DisplayName;
                clone.VaultSubFolder = clone.Id;
                clone.TargetType = orig.TargetType;
                clone.SizeBytes = orig.SizeBytes;
                clone.FileCount = orig.FileCount;
                clone.PreviewPath = orig.PreviewPath;

                string srcItemDir = Path.Combine(srcProfDir, orig.VaultSubFolder);
                string dstItemDir = Path.Combine(dstProfDir, clone.VaultSubFolder);
                if (Directory.Exists(srcItemDir))
                    CopyDirectory(srcItemDir, dstItemDir);

                copy.Items.Add(clone);
            }

            db.Profiles.Add(copy);
            SaveDatabase();
            RefreshProfilesList();
            lstProfiles.SelectedIndex = db.Profiles.Count - 1;
            AppendLog((IsEn ? "Profile duplicated: " : "Perfil duplicado: ") + copy.Name);
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            if (string.Equals(prof.Id, db.ActiveProfileId, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    IsEn
                        ? "This profile is currently active in the game. Click 'Disable All (Clean Game)' or activate another profile before deleting it."
                        : "Este perfil est\u00e1 ativo no jogo no momento. Clique em 'Desativar Tudo (Jogo Limpo)' ou ative outro perfil antes de excluir este.",
                    IsEn ? "Active Profile" : "Perfil Ativo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            DialogResult dr = MessageBox.Show(
                IsEn
                    ? "Are you sure you want to delete the profile '" + prof.Name + "' and all mods stored inside it?"
                    : "Tem certeza que deseja excluir o perfil '" + prof.Name + "' e todos os mods salvos nele?",
                IsEn ? "Delete Profile" : "Excluir Perfil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (dr != DialogResult.Yes) return;

            string dir = Path.Combine(profilesVaultDir, prof.Id);
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }

            db.Profiles.Remove(prof);
            SaveDatabase();
            RefreshProfilesList();
            AppendLog((IsEn ? "Profile deleted: " : "Perfil exclu\u00eddo: ") + prof.Name);
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = IsEn
                    ? "Select mods (.iwd, .iwi, .gsc, .csc, .ff, .zip, .7z, .rar)"
                    : "Selecione mods (.iwd, .iwi, .gsc, .csc, .ff, .zip, .7z, .rar)";
                ofd.Multiselect = true;
                ofd.Filter = IsEn
                    ? "All Supported Mods (*.iwd;*.iwi;*.gsc;*.csc;*.ff;*.zip;*.7z;*.rar)|*.iwd;*.iwi;*.gsc;*.csc;*.ff;*.zip;*.7z;*.rar|All files (*.*)|*.*"
                    : "Todos os Mods Suportados (*.iwd;*.iwi;*.gsc;*.csc;*.ff;*.zip;*.7z;*.rar)|*.iwd;*.iwi;*.gsc;*.csc;*.ff;*.zip;*.7z;*.rar|Todos os arquivos (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    ImportPathsIntoProfile(prof, ofd.FileNames);
                }
            }
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = IsEn
                    ? "Select a mod or custom map folder (camos/images, scripts, custom ZM/MP map, etc.)"
                    : "Selecione uma pasta de mod ou mapa customizado (camuflagens/images, scripts, mapa ZM/MP, etc.)";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    ImportPathsIntoProfile(prof, new string[] { fbd.SelectedPath });
                }
            }
        }

        private void LstModItems_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void LstModItems_DragDrop(object sender, DragEventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropped != null && dropped.Length > 0)
            {
                ImportPathsIntoProfile(prof, dropped);
            }
        }

        private void ImportPathsIntoProfile(ModProfile prof, string[] paths)
        {
            int countBefore = prof.Items.Count;
            string tempWork = Path.Combine(Path.GetTempPath(), "PlutoImport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempWork);

            try
            {
                lblStatus.ForeColor = AccentOrange;
                lblStatus.Text = IsEn
                    ? "Analyzing and importing mods into '" + prof.Name + "'..."
                    : "Analisando e importando mods para o perfil '" + prof.Name + "'...";
                progressBar.Value = 15;
                Application.DoEvents();

                List<string> looseIwiFiles = new List<string>();

                for (int i = 0; i < paths.Length; i++)
                {
                    string p = paths[i];
                    if (File.Exists(p) && string.Equals(Path.GetExtension(p), ".iwi", StringComparison.OrdinalIgnoreCase))
                    {
                        looseIwiFiles.Add(p);
                    }
                    else
                    {
                        ImportSingleSource(prof, p, tempWork);
                    }

                    int pct = 15 + (int)(((i + 1) * 75) / Math.Max(1, paths.Length));
                    if (pct > 95) pct = 95;
                    progressBar.Value = pct;
                    Application.DoEvents();
                }

                if (looseIwiFiles.Count > 0)
                {
                    PackLooseIwiListIntoProfile(prof, looseIwiFiles, "Camos_" + SanitizeFileName(prof.Name), tempWork);
                }

                int addedCount = prof.Items.Count - countBefore;
                SaveDatabase();
                RefreshProfilesList();
                RefreshSelectedProfileView();

                if (addedCount > 0)
                {
                    progressBar.Value = 100;
                    lblStatus.ForeColor = AccentGreen;
                    lblStatus.Text = IsEn
                        ? addedCount + " mod(s) imported into '" + prof.Name + "'! Click ACTIVATE PROFILE IN GAME."
                        : addedCount + " mod(s) importado(s) para '" + prof.Name + "'! Clique em 'ATIVAR PERFIL NO JOGO'.";
                }
                else
                {
                    progressBar.Value = 0;
                    lblStatus.ForeColor = AccentCoral;
                    lblStatus.Text = IsEn
                        ? "No mod files (.iwd, .iwi, .gsc, .ff) found in the selected item."
                        : "Nenhum arquivo de mod (.iwd, .iwi, .gsc, .ff) encontrado na pasta selecionada.";
                    AppendLog(IsEn
                        ? "[WARN] No recognized mod files found in selected items."
                        : "[AVISO] Nenhum arquivo de mod reconhecido nos itens selecionados.");
                    MessageBox.Show(
                        IsEn
                            ? "No valid mod files (.iwd, .iwi, .gsc, .csc, .ff, or .zip) were found inside the selected item.\n\nMake sure .rar files have 7-Zip/WinRAR installed or extract them first."
                            : "Nenhum arquivo de mod v\u00e1lido (.iwd, .iwi, .gsc, .csc, .ff ou .zip) foi encontrado dentro da pasta selecionada.\n\nVerifique se os arquivos dentro da pasta n\u00e3o est\u00e3o compactados em .rar sem WinRAR/7-Zip instalado.",
                        IsEn ? "No Mods Found" : "Nenhum Mod Encontrado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = AccentCoral;
                lblStatus.Text = IsEn ? "Error importing mod." : "Erro ao importar mod.";
                AppendLog("[ERRO] " + ex.Message);
                MessageBox.Show((IsEn ? "Error importing mod:\n" : "Erro ao importar mod:\n") + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (Directory.Exists(tempWork)) Directory.Delete(tempWork, true); } catch { }
            }
        }

        private void PackLooseIwiListIntoProfile(ModProfile prof, List<string> iwiFiles, string baseTitle, string tempWork)
        {
            if (iwiFiles == null || iwiFiles.Count == 0) return;

            string cleanTitle = SanitizeFileName(baseTitle);
            string stageRoot = Path.Combine(tempWork, "iwi_stage_" + Guid.NewGuid().ToString("N"));
            string stageImg = Path.Combine(stageRoot, "images");
            Directory.CreateDirectory(stageImg);

            HashSet<string> writtenNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < iwiFiles.Count; i++)
            {
                string srcFile = iwiFiles[i];
                string origName = Path.GetFileName(srcFile);
                if (!writtenNames.Contains(origName))
                {
                    File.Copy(srcFile, Path.Combine(stageImg, origName), true);
                    writtenNames.Add(origName);
                }

                string lowerName = origName.ToLowerInvariant();
                if (!writtenNames.Contains(lowerName))
                {
                    File.Copy(srcFile, Path.Combine(stageImg, lowerName), true);
                    writtenNames.Add(lowerName);
                }
            }

            ModEntryItem item = CreateVaultItem(prof, baseTitle + " (" + iwiFiles.Count + (IsEn ? " textures)" : " texturas)"), ModTargetType.MainIwd);
            string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
            Directory.CreateDirectory(itemVaultDir);

            string iwdFileName = (cleanTitle.StartsWith("z_", StringComparison.OrdinalIgnoreCase) ? cleanTitle : "z_" + cleanTitle) + "_" + item.Id.Substring(0, 4) + ".iwd";
            string outIwd = Path.Combine(itemVaultDir, iwdFileName);
            ZipFile.CreateFromDirectory(stageRoot, outIwd, CompressionLevel.NoCompression, false);

            FinalizeVaultItemStats(item, itemVaultDir, "main\\" + iwdFileName);
            prof.Items.Add(item);
            AppendLog("Importado [MAIN/IWD]: " + item.DisplayName + " -> main\\" + iwdFileName);
        }

        private void ImportSingleSource(ModProfile prof, string path, string tempWork)
        {
            if (File.Exists(path))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string fileName = Path.GetFileName(path);
                string nameNoExt = Path.GetFileNameWithoutExtension(path);

                if (ext == ".zip" || ext == ".7z" || ext == ".rar")
                {
                    string extractDir = Path.Combine(tempWork, "arc_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(extractDir);

                    if (ext == ".zip")
                        ZipFile.ExtractToDirectory(path, extractDir);
                    else
                        ExtractWithExternalArchiver(path, extractDir);

                    string[] topFiles = Directory.GetFiles(extractDir);
                    string[] topDirs = Directory.GetDirectories(extractDir);
                    if (topFiles.Length == 0 && topDirs.Length == 1)
                        ImportDirectorySmart(prof, topDirs[0], Path.GetFileName(topDirs[0]), tempWork);
                    else
                        ImportDirectorySmart(prof, extractDir, nameNoExt, tempWork);
                }
                else if (ext == ".iwd")
                {
                    ModEntryItem item = CreateVaultItem(prof, nameNoExt, ModTargetType.MainIwd);
                    string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                    Directory.CreateDirectory(itemVaultDir);

                    string destIwd = Path.Combine(itemVaultDir, fileName);
                    NormalizeAndCopyIwd(path, destIwd, tempWork);

                    FinalizeVaultItemStats(item, itemVaultDir, "main\\" + fileName);
                    prof.Items.Add(item);
                    AppendLog("Importado [MAIN/IWD]: " + fileName);
                }
                else if (ext == ".iwi")
                {
                    List<string> singleList = new List<string>();
                    singleList.Add(path);
                    PackLooseIwiListIntoProfile(prof, singleList, nameNoExt, tempWork);
                }
                else if (ext == ".gsc" || ext == ".csc")
                {
                    ModTargetType detectedScriptTarget = DetectScriptMode(path);
                    ModEntryItem item = CreateVaultItem(prof, fileName, detectedScriptTarget);
                    string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                    Directory.CreateDirectory(itemVaultDir);

                    File.Copy(path, Path.Combine(itemVaultDir, fileName), true);
                    string sub = detectedScriptTarget == ModTargetType.ScriptsMp ? "scripts\\mp\\" : "scripts\\zm\\";
                    FinalizeVaultItemStats(item, itemVaultDir, sub + fileName);
                    prof.Items.Add(item);
                    AppendLog("Importado [" + (detectedScriptTarget == ModTargetType.ScriptsMp ? "SCRIPTS/MP" : "SCRIPTS/ZM") + "]: " + fileName);
                }
                else if (ext == ".ff")
                {
                    ModEntryItem item = CreateVaultItem(prof, fileName, ModTargetType.ModFastFile);
                    string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                    Directory.CreateDirectory(itemVaultDir);

                    File.Copy(path, Path.Combine(itemVaultDir, "mod.ff"), true);
                    FinalizeVaultItemStats(item, itemVaultDir, "mods\\" + SanitizeFileName(prof.Name) + "\\mod.ff");
                    prof.Items.Add(item);
                    AppendLog("Importado [MODS/FF]: " + fileName);
                }
            }
            else if (Directory.Exists(path))
            {
                string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string dirName = Path.GetFileName(trimmed);
                ImportDirectorySmart(prof, trimmed, dirName, tempWork);
            }
        }

        private void ExtractWithExternalArchiver(string archivePath, string destDir)
        {
            string[] sevenZipPaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
            };

            for (int i = 0; i < sevenZipPaths.Length; i++)
            {
                if (File.Exists(sevenZipPaths[i]))
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = sevenZipPaths[i];
                    psi.Arguments = "x \"" + archivePath + "\" -o\"" + destDir + "\" -y";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                    if (p.ExitCode == 0) return;
                }
            }

            string[] winRarPaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinRAR", "UnRAR.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinRAR", "UnRAR.exe")
            };

            for (int i = 0; i < winRarPaths.Length; i++)
            {
                if (File.Exists(winRarPaths[i]))
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = winRarPaths[i];
                    psi.Arguments = "x -y \"" + archivePath + "\" \"" + destDir + "\\\"";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                    if (p.ExitCode == 0) return;
                }
            }

            throw new Exception(IsEn
                ? "To import .rar or .7z archives directly, extract them first or install 7-Zip/WinRAR. .zip files work natively."
                : "Para importar arquivos .rar ou .7z diretamente, extraia antes ou instale o 7-Zip/WinRAR. Arquivos .zip funcionam nativamente.");
        }

        private void ImportDirectorySmart(ModProfile prof, string dirPath, string labelName, string tempWork)
        {
            string lowerName = labelName.ToLowerInvariant();
            AppendLog("[SCAN] Analisando pasta: " + labelName);

            // If user drops a 'usermaps' or 'mods' root container, import each map/mod subfolder into mods\<name>
            if (lowerName == "usermaps" || lowerName == "mods")
            {
                string[] mapDirs = Directory.GetDirectories(dirPath);
                for (int i = 0; i < mapDirs.Length; i++)
                {
                    string mName = Path.GetFileName(mapDirs[i]);
                    ImportAsCustomMapMod(prof, mapDirs[i], mName);
                }
                return;
            }

            if (lowerName == "scripts")
            {
                ModEntryItem item = CreateVaultItem(prof, "Scripts (" + labelName + ")", ModTargetType.ScriptsRoot);
                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                CopyDirectory(dirPath, itemVaultDir);
                FinalizeVaultItemStats(item, itemVaultDir, "scripts\\...");
                prof.Items.Add(item);
                AppendLog("Importado [SCRIPTS]: Pasta scripts completa");
                return;
            }

            if (IsAssetFolder(labelName))
            {
                string parentName = Path.GetFileName(Path.GetDirectoryName(dirPath));
                string cleanTitle = (!string.IsNullOrEmpty(parentName) && parentName.Length > 2 && !parentName.StartsWith("arc_"))
                    ? parentName + "_" + labelName
                    : "Pack_" + labelName;

                ModEntryItem item = CreateVaultItem(prof, cleanTitle, ModTargetType.MainIwd);
                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                Directory.CreateDirectory(itemVaultDir);

                string stageRoot = Path.Combine(tempWork, "asset_stage_" + Guid.NewGuid().ToString("N"));
                CopyDirectory(dirPath, Path.Combine(stageRoot, labelName));

                string iwdName = "z_" + SanitizeFileName(cleanTitle) + "_" + item.Id.Substring(0, 4) + ".iwd";
                string outIwd = Path.Combine(itemVaultDir, iwdName);
                ZipFile.CreateFromDirectory(stageRoot, outIwd, CompressionLevel.NoCompression, false);

                FinalizeVaultItemStats(item, itemVaultDir, "main\\" + iwdName);
                prof.Items.Add(item);
                AppendLog("Importado [MAIN/IWD]: " + cleanTitle + " -> main\\" + iwdName);
                return;
            }

            string[] allFiles = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
            if (allFiles.Length == 0) return;

            bool hasMapFastFiles = false;
            bool hasModFastFile = false;
            int countIwd = 0;
            string detectedMapFfDir = null;

            for (int i = 0; i < allFiles.Length; i++)
            {
                string fName = Path.GetFileName(allFiles[i]).ToLowerInvariant();
                string fExt = Path.GetExtension(fName);
                if (fExt == ".ff")
                {
                    if (fName == "mod.ff")
                    {
                        hasModFastFile = true;
                        if (detectedMapFfDir == null)
                            detectedMapFfDir = Path.GetDirectoryName(allFiles[i]);
                    }
                    else
                    {
                        hasMapFastFiles = true;
                        detectedMapFfDir = Path.GetDirectoryName(allFiles[i]);
                    }
                }
                else if (fExt == ".iwd")
                {
                    countIwd++;
                }
            }

            bool looksLikeMapName = lowerName.StartsWith("zm_") || lowerName.StartsWith("mp_");
            if (hasMapFastFiles || (looksLikeMapName && (countIwd > 0 || hasModFastFile)))
            {
                string actualMapDir = dirPath;
                string actualMapName = labelName;
                if (!string.IsNullOrEmpty(detectedMapFfDir) && Directory.Exists(detectedMapFfDir))
                {
                    string nestedFolderName = Path.GetFileName(detectedMapFfDir);
                    if (!string.IsNullOrEmpty(nestedFolderName)
                        && !nestedFolderName.StartsWith("arc_", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(nestedFolderName, "usermaps", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(nestedFolderName, "mods", StringComparison.OrdinalIgnoreCase))
                    {
                        actualMapDir = detectedMapFfDir;
                        actualMapName = nestedFolderName;
                    }
                }

                ImportAsCustomMapMod(prof, actualMapDir, actualMapName);
                return;
            }

            if (hasModFastFile)
            {
                string cleanModFolder = SanitizeFileName(labelName);
                ModEntryItem item = CreateVaultItem(prof, labelName + " (mod.ff)", ModTargetType.ModFastFile);
                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder, cleanModFolder);
                CopyDirectory(dirPath, itemVaultDir);
                FinalizeVaultItemStats(item, Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder), "mods\\" + cleanModFolder + "\\mod.ff");
                prof.Items.Add(item);
                AppendLog("Importado [MODS/FF]: " + labelName + " -> mods\\" + cleanModFolder);
                return;
            }

            string[] topSubDirs = Directory.GetDirectories(dirPath);
            string[] topFiles = Directory.GetFiles(dirPath);
            bool hasAssetSub = false;
            bool hasOtherContent = false;

            for (int i = 0; i < topSubDirs.Length; i++)
            {
                string sn = Path.GetFileName(topSubDirs[i]);
                if (IsAssetFolder(sn)) hasAssetSub = true;
                else hasOtherContent = true;
            }
            for (int i = 0; i < topFiles.Length; i++)
            {
                string ext = Path.GetExtension(topFiles[i]).ToLowerInvariant();
                if (ext != ".txt" && ext != ".md" && ext != ".url" && ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".ini")
                    hasOtherContent = true;
            }

            if (hasAssetSub && !hasOtherContent)
            {
                ModEntryItem item = CreateVaultItem(prof, labelName, ModTargetType.MainIwd);
                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                Directory.CreateDirectory(itemVaultDir);

                string stageRoot = Path.Combine(tempWork, "subasset_stage_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stageRoot);
                for (int i = 0; i < topSubDirs.Length; i++)
                {
                    string sn = Path.GetFileName(topSubDirs[i]);
                    if (IsAssetFolder(sn))
                        CopyDirectory(topSubDirs[i], Path.Combine(stageRoot, sn));
                }

                string iwdName = labelName.StartsWith("z_", StringComparison.OrdinalIgnoreCase)
                    ? SanitizeFileName(labelName) + ".iwd"
                    : "z_" + SanitizeFileName(labelName) + ".iwd";
                string outIwd = Path.Combine(itemVaultDir, iwdName);
                ZipFile.CreateFromDirectory(stageRoot, outIwd, CompressionLevel.NoCompression, false);

                FinalizeVaultItemStats(item, itemVaultDir, "main\\" + iwdName);
                prof.Items.Add(item);
                AppendLog("Importado [MAIN/IWD]: " + labelName + " -> main\\" + iwdName);
                return;
            }

            List<string> folderLooseIwi = new List<string>();
            for (int i = 0; i < topFiles.Length; i++)
            {
                string ext = Path.GetExtension(topFiles[i]).ToLowerInvariant();
                if (ext == ".iwi")
                    folderLooseIwi.Add(topFiles[i]);
                else
                    ImportSingleSource(prof, topFiles[i], tempWork);
            }

            if (folderLooseIwi.Count > 0)
            {
                PackLooseIwiListIntoProfile(prof, folderLooseIwi, labelName, tempWork);
            }

            int itemsBeforeSub = prof.Items.Count;
            for (int i = 0; i < topSubDirs.Length; i++)
            {
                string subName = Path.GetFileName(topSubDirs[i]);
                ImportDirectorySmart(prof, topSubDirs[i], subName, tempWork);
            }

            if (prof.Items.Count == itemsBeforeSub && folderLooseIwi.Count == 0 && allFiles.Length > 0)
            {
                List<string> nestedIwi = new List<string>();
                for (int i = 0; i < allFiles.Length; i++)
                {
                    if (string.Equals(Path.GetExtension(allFiles[i]), ".iwi", StringComparison.OrdinalIgnoreCase))
                        nestedIwi.Add(allFiles[i]);
                }
                if (nestedIwi.Count > 0)
                {
                    PackLooseIwiListIntoProfile(prof, nestedIwi, labelName + "_Texturas", tempWork);
                }
            }
        }

        private void ImportAsCustomMapMod(ModProfile prof, string mapSourceDir, string mapFolderName)
        {
            string cleanMapName = SanitizeFileName(mapFolderName);
            ModEntryItem item = CreateVaultItem(prof, "Mapa: " + cleanMapName, ModTargetType.CustomMapMod);
            string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder, cleanMapName);
            CopyDirectory(mapSourceDir, itemVaultDir);
            FinalizeVaultItemStats(item, Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder), "mods\\" + cleanMapName);
            prof.Items.Add(item);
            AppendLog("Importado [MAPA/MODS]: " + cleanMapName + " -> mods\\" + cleanMapName + " (ativar no menu Mods do jogo)");
        }

        private void NormalizeAndCopyIwd(string sourceIwd, string destIwd, string tempWork)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(sourceIwd))
                {
                    bool hasRootAssetFolder = false;
                    HashSet<string> topSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string norm = entry.FullName.Replace('\\', '/');
                        int slashIdx = norm.IndexOf('/');
                        if (slashIdx > 0)
                        {
                            string top = norm.Substring(0, slashIdx);
                            topSegments.Add(top);
                            if (IsAssetFolder(top))
                                hasRootAssetFolder = true;
                        }
                    }

                    if (hasRootAssetFolder || topSegments.Count != 1)
                    {
                        File.Copy(sourceIwd, destIwd, true);
                        return;
                    }

                    string singleWrapper = null;
                    foreach (string s in topSegments) singleWrapper = s;

                    string fixDir = Path.Combine(tempWork, "fix_iwd_" + Guid.NewGuid().ToString("N"));
                    ZipFile.ExtractToDirectory(sourceIwd, fixDir);
                    string innerDir = Path.Combine(fixDir, singleWrapper);

                    if (Directory.Exists(innerDir))
                    {
                        ZipFile.CreateFromDirectory(innerDir, destIwd, CompressionLevel.NoCompression, false);
                        AppendLog("  -> Estrutura interna do .iwd corrigida automaticamente (raiz limpa).");
                        return;
                    }
                }
            }
            catch { }

            File.Copy(sourceIwd, destIwd, true);
        }

        private ModTargetType DetectScriptMode(string scriptFilePath)
        {
            try
            {
                string parentFolder = Path.GetFileName(Path.GetDirectoryName(scriptFilePath)).ToLowerInvariant();
                if (parentFolder == "mp") return ModTargetType.ScriptsMp;
                if (parentFolder == "zm") return ModTargetType.ScriptsZm;

                string content = File.ReadAllText(scriptFilePath).ToLowerInvariant();
                int zmScore = 0;
                int mpScore = 0;

                if (content.Contains("zombies") || content.Contains("_zm") || content.Contains("zombie_")) zmScore += 3;
                if (content.Contains("perk_") || content.Contains("mystery_box") || content.Contains("pack_a_punch")) zmScore += 2;
                if (content.Contains("gametypes") || content.Contains("globallogic") || content.Contains("teambased")) mpScore += 3;

                if (mpScore > zmScore)
                    return ModTargetType.ScriptsMp;
            }
            catch { }
            return ModTargetType.ScriptsZm;
        }

        private ModEntryItem CreateVaultItem(ModProfile prof, string displayName, ModTargetType target)
        {
            ModEntryItem item = new ModEntryItem();
            item.Id = Guid.NewGuid().ToString("N");
            item.DisplayName = displayName;
            item.VaultSubFolder = item.Id;
            item.TargetType = target;
            return item;
        }

        private void FinalizeVaultItemStats(ModEntryItem item, string folderPath, string previewPath)
        {
            item.PreviewPath = previewPath;
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                item.FileCount = files.Length;
                long size = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    size += new FileInfo(files[i]).Length;
                }
                item.SizeBytes = size;
            }
            ModContentAnalyzer.AnalyzeVaultItem(folderPath, item);
        }

        private void BtnChangeDest_Click(object sender, EventArgs e)
        {
            if (lstModItems.SelectedIndices.Count == 0)
            {
                MessageBox.Show(
                    IsEn ? "Select one or more mods in the list first to change their destination folder." : "Selecione um ou mais mods na lista primeiro para alterar a pasta de destino.",
                    IsEn ? "Notice" : "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }
            ctxDestMenu.Show(btnChangeDest, new Point(0, btnChangeDest.Height));
        }

        private void ChangeSelectedModDestination(ModTargetType newTarget)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null || lstModItems.SelectedIndices.Count == 0) return;

            for (int s = 0; s < lstModItems.SelectedIndices.Count; s++)
            {
                int idx = lstModItems.SelectedIndices[s];
                if (idx < 0 || idx >= prof.Items.Count) continue;

                ModEntryItem item = prof.Items[idx];
                item.TargetType = newTarget;

                string itemDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                string firstFile = "";
                string singleSubDir = "";
                if (Directory.Exists(itemDir))
                {
                    string[] subDirs = Directory.GetDirectories(itemDir);
                    string[] topFiles = Directory.GetFiles(itemDir);
                    if (topFiles.Length == 0 && subDirs.Length == 1)
                        singleSubDir = Path.GetFileName(subDirs[0]);

                    string[] files = Directory.GetFiles(itemDir, "*", SearchOption.AllDirectories);
                    if (files.Length > 0) firstFile = Path.GetFileName(files[0]);
                }

                switch (newTarget)
                {
                    case ModTargetType.MainIwd:
                        item.PreviewPath = "main\\" + firstFile;
                        break;
                    case ModTargetType.ScriptsZm:
                        item.PreviewPath = "scripts\\zm\\" + firstFile;
                        break;
                    case ModTargetType.ScriptsMp:
                        item.PreviewPath = "scripts\\mp\\" + firstFile;
                        break;
                    case ModTargetType.ScriptsRoot:
                        item.PreviewPath = "scripts\\...";
                        break;
                    case ModTargetType.CustomMapMod:
                    {
                        string mapName = !string.IsNullOrEmpty(singleSubDir) ? singleSubDir : SanitizeFileName(StripMapPrefix(item.DisplayName));
                        item.PreviewPath = "mods\\" + mapName;
                        break;
                    }
                    case ModTargetType.ModFastFile:
                    {
                        string modFolder = !string.IsNullOrEmpty(singleSubDir) ? singleSubDir : SanitizeFileName(prof.Name);
                        item.PreviewPath = "mods\\" + modFolder + "\\mod.ff";
                        break;
                    }
                    case ModTargetType.Usermap:
                    {
                        string uName = !string.IsNullOrEmpty(singleSubDir) ? singleSubDir : SanitizeFileName(StripMapPrefix(item.DisplayName));
                        item.PreviewPath = "usermaps\\" + uName;
                        break;
                    }
                }

                string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                ModContentAnalyzer.AnalyzeVaultItem(itemVaultDir, item);
                AppendLog((IsEn ? "Target updated: " : "Destino alterado: ") + item.DisplayName + " -> " + item.PreviewPath);
            }

            SaveDatabase();
            RefreshSelectedProfileView();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null || lstModItems.SelectedIndices.Count == 0) return;

            List<int> indices = new List<int>();
            for (int i = 0; i < lstModItems.SelectedIndices.Count; i++)
                indices.Add(lstModItems.SelectedIndices[i]);
            indices.Sort();
            indices.Reverse();

            for (int i = 0; i < indices.Count; i++)
            {
                int idx = indices[i];
                if (idx < 0 || idx >= prof.Items.Count) continue;
                ModEntryItem item = prof.Items[idx];
                string dir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
                prof.Items.RemoveAt(idx);
                AppendLog((IsEn ? "Removed from profile: " : "Item removido do perfil: ") + item.DisplayName);
            }

            SaveDatabase();
            RefreshProfilesList();
            RefreshSelectedProfileView();
        }

        private void BtnActivateProfile_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null) return;

            if (prof.Items.Count == 0)
            {
                MessageBox.Show(
                    IsEn ? "This profile is empty. Add mods (camos, scripts, or custom maps) before activating." : "Este perfil est\u00e1 vazio. Adicione mods (camuflagens, scripts ou mapas) antes de ativar.",
                    IsEn ? "Empty Profile" : "Perfil Vazio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            string t6Dir = txtT6Path.Text.Trim();
            if (string.IsNullOrEmpty(t6Dir))
            {
                MessageBox.Show(
                    IsEn ? "Configure the Plutonium storage\\t6 folder path first." : "Configure o caminho da pasta storage\\t6 do Plutonium primeiro.",
                    IsEn ? "Invalid Path" : "Caminho Inv\u00e1lido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            SetControlsEnabled(false);
            try
            {
                progressBar.Value = 10;
                lblStatus.ForeColor = AccentOrange;
                lblStatus.Text = IsEn ? "Cleaning previous profile mods..." : "Limpando mods do perfil anterior...";
                AppendLog("=== Ativando perfil: " + prof.Name + " ===");

                UninstallActiveManifest(t6Dir);

                Directory.CreateDirectory(t6Dir);
                List<string> newlyInstalledRelPaths = new List<string>();
                List<string> installedCustomMapNames = new List<string>();
                bool hasModFf = false;

                for (int i = 0; i < prof.Items.Count; i++)
                {
                    ModEntryItem item = prof.Items[i];
                    string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                    if (!Directory.Exists(itemVaultDir)) continue;

                    string targetBaseDir = ResolveTargetBaseDir(t6Dir, prof, item, itemVaultDir);
                    if (item.TargetType == ModTargetType.CustomMapMod)
                    {
                        string mapClean = SanitizeFileName(StripMapPrefix(item.DisplayName));
                        if (!installedCustomMapNames.Contains(mapClean))
                            installedCustomMapNames.Add(mapClean);
                    }
                    else if (item.TargetType == ModTargetType.ModFastFile)
                    {
                        hasModFf = true;
                    }

                    Directory.CreateDirectory(targetBaseDir);
                    string[] files = Directory.GetFiles(itemVaultDir, "*", SearchOption.AllDirectories);
                    for (int f = 0; f < files.Length; f++)
                    {
                        string relToItem = GetRelativePath(itemVaultDir, files[f]);
                        string destFull = Path.Combine(targetBaseDir, relToItem);
                        string destParent = Path.GetDirectoryName(destFull);
                        if (!string.IsNullOrEmpty(destParent))
                            Directory.CreateDirectory(destParent);

                        File.Copy(files[f], destFull, true);
                        string relToT6 = GetRelativePath(t6Dir, destFull);
                        newlyInstalledRelPaths.Add(relToT6);
                        AppendLog("  [+] " + relToT6);
                    }

                    int pct = 20 + (int)(((i + 1) * 75) / Math.Max(1, prof.Items.Count));
                    if (pct > 95) pct = 95;
                    progressBar.Value = pct;
                    Application.DoEvents();
                }

                File.WriteAllLines(activeManifestPath, newlyInstalledRelPaths.ToArray(), Encoding.UTF8);
                db.T6Directory = t6Dir;
                db.ActiveProfileId = prof.Id;
                SaveDatabase();

                missingItemIds.Clear();
                currentSyncState = DiskSyncState.SyncedOk;

                progressBar.Value = 100;
                lblStatus.ForeColor = AccentGreen;
                lblStatus.Text = IsEn
                    ? "Profile '" + prof.Name + "' active in Plutonium T6 (" + newlyInstalledRelPaths.Count + " file(s) installed)."
                    : "Perfil '" + prof.Name + "' ativo no Plutonium T6 (" + newlyInstalledRelPaths.Count + " arquivo(s) instalados).";
                AppendLog("Perfil '" + prof.Name + "' ativado com sucesso!");

                UpdateActiveBanner();
                RefreshSelectedProfileView();

                StringBuilder msg = new StringBuilder();
                if (IsEn)
                {
                    msg.AppendLine("Profile '" + prof.Name + "' is now active in Plutonium T6!");
                    msg.AppendLine("Total files applied: " + newlyInstalledRelPaths.Count);
                    if (installedCustomMapNames.Count > 0)
                    {
                        string mapList = string.Join(", ", installedCustomMapNames.ToArray());
                        msg.AppendLine();
                        msg.AppendLine("IMPORTANT -- CUSTOM MAP INSTRUCTIONS (ZOMBIES & MULTIPLAYER):");
                        msg.AppendLine("Custom maps are installed in: storage\\t6\\mods\\ (" + mapList + ")");
                        msg.AppendLine("1. Open Plutonium BO2 (Zombies or Multiplayer).");
                        msg.AppendLine("2. On the MAIN MENU, click 'Mods' and load: " + mapList + ".");
                        msg.AppendLine("3. Start a Custom Game or open the console (~) and run: map <map_name>.");
                    }
                    else if (hasModFf)
                    {
                        msg.AppendLine();
                        msg.AppendLine("Note: This profile includes a mod.ff file. Load it from the in-game 'Mods' menu if required.");
                    }
                }
                else
                {
                    msg.AppendLine("Perfil '" + prof.Name + "' ativado no Plutonium T6!");
                    msg.AppendLine("Total de arquivos aplicados: " + newlyInstalledRelPaths.Count);
                    if (installedCustomMapNames.Count > 0)
                    {
                        string mapList = string.Join(", ", installedCustomMapNames.ToArray());
                        msg.AppendLine();
                        msg.AppendLine("AVISO IMPORTANTE -- COMO JOGAR MAPAS CUSTOMIZADOS (ZOMBIES E MULTIPLAYER):");
                        msg.AppendLine("Os mapas foram instalados em: storage\\t6\\mods\\ (" + mapList + ")");
                        msg.AppendLine("1. Abra o Plutonium BO2 (Zombies ou Multiplayer).");
                        msg.AppendLine("2. No MENU PRINCIPAL, clique em 'Mods' e ative o mapa: " + mapList + ".");
                        msg.AppendLine("3. Depois de ativar, inicie uma Partida Personalizada ou abra o console (') e digite: map <nome_do_mapa>.");
                    }
                    else if (hasModFf)
                    {
                        msg.AppendLine();
                        msg.AppendLine("Nota: Este perfil inclui um arquivo mod.ff. Ative-o tamb\u00e9m no menu 'Mods' do jogo se necess\u00e1rio.");
                    }
                }

                MessageBox.Show(
                    msg.ToString(),
                    IsEn ? "Profile Activated" : "Perfil Ativado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = AccentCoral;
                lblStatus.Text = IsEn ? "Error activating profile." : "Erro ao ativar perfil.";
                AppendLog("[ERRO] " + ex.Message);
                MessageBox.Show((IsEn ? "Error activating profile:\n" : "Erro ao ativar perfil:\n") + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void BtnDeactivateAll_Click(object sender, EventArgs e)
        {
            string t6Dir = txtT6Path.Text.Trim();
            SetControlsEnabled(false);
            try
            {
                AppendLog("=== Desativando todos os mods (Restaurando Jogo Limpo) ===");
                UninstallActiveManifest(t6Dir);

                db.ActiveProfileId = "";
                SaveDatabase();

                missingItemIds.Clear();
                currentSyncState = DiskSyncState.Clean;

                progressBar.Value = 0;
                lblStatus.ForeColor = TextMuted;
                lblStatus.Text = IsEn
                    ? "All managed mods/maps removed. Clean game (Vanilla)."
                    : "Todos os mods gerenciados foram removidos. Jogo limpo (Vanilla).";

                UpdateActiveBanner();
                RefreshSelectedProfileView();
                MessageBox.Show(
                    IsEn ? "All managed mods and maps have been removed from the game!" : "Todos os mods e mapas do perfil ativo foram removidos do jogo!",
                    IsEn ? "Clean Game" : "Jogo Limpo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show((IsEn ? "Error disabling mods:\n" : "Erro ao desativar mods:\n") + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void UninstallActiveManifest(string t6Dir)
        {
            if (!File.Exists(activeManifestPath)) return;

            string[] lines = File.ReadAllLines(activeManifestPath, Encoding.UTF8);
            HashSet<string> dirsToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                string rel = lines[i].Trim();
                if (string.IsNullOrEmpty(rel)) continue;
                string full = Path.Combine(t6Dir, rel);
                if (File.Exists(full))
                {
                    File.Delete(full);
                    AppendLog("  [-] Removido: " + rel);
                }
                string parent = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(parent))
                    dirsToCheck.Add(parent);
            }

            string[] protectedRoots = new string[]
            {
                t6Dir,
                Path.Combine(t6Dir, "main"),
                Path.Combine(t6Dir, "scripts"),
                Path.Combine(t6Dir, "scripts", "zm"),
                Path.Combine(t6Dir, "scripts", "mp"),
                Path.Combine(t6Dir, "usermaps"),
                Path.Combine(t6Dir, "mods")
            };

            foreach (string dir in dirsToCheck)
            {
                CleanEmptyDirectoriesUpward(dir, protectedRoots);
            }

            File.Delete(activeManifestPath);
        }

        private void CleanEmptyDirectoriesUpward(string dir, string[] protectedRoots)
        {
            try
            {
                while (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    for (int i = 0; i < protectedRoots.Length; i++)
                    {
                        if (string.Equals(dir.TrimEnd('\\'), protectedRoots[i].TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                    if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                    {
                        Directory.Delete(dir);
                        dir = Path.GetDirectoryName(dir);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch { }
        }

        private void BtnExportExe_Click(object sender, EventArgs e)
        {
            ModProfile prof = GetSelectedProfile();
            if (prof == null || prof.Items.Count == 0)
            {
                MessageBox.Show(
                    IsEn ? "Select a profile that contains at least 1 mod to export as .exe." : "Selecione um perfil que contenha pelo menos 1 mod para exportar como .exe.",
                    IsEn ? "Empty Profile" : "Perfil Vazio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            string csc64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
            string csc32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework", "v4.0.30319", "csc.exe");
            string cscPath = File.Exists(csc64) ? csc64 : (File.Exists(csc32) ? csc32 : null);

            if (cscPath == null)
            {
                MessageBox.Show(
                    IsEn ? ".NET Framework compiler (csc.exe) not found on Windows." : "Compilador .NET (csc.exe) n\u00e3o encontrado no Windows.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            string defaultExeName = "Instalador_" + SanitizeFileName(prof.Name) + ".exe";
            string outExePath;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = IsEn
                    ? "Save Standalone Installer (.exe) for '" + prof.Name + "'"
                    : "Salvar Instalador \u00danico (.exe) do Perfil '" + prof.Name + "'";
                sfd.Filter = "Aplicativo Execut\u00e1vel (*.exe)|*.exe";
                sfd.FileName = defaultExeName;
                if (sfd.ShowDialog() != DialogResult.OK) return;
                outExePath = sfd.FileName;
            }

            SetControlsEnabled(false);
            string tempExportDir = Path.Combine(Path.GetTempPath(), "PlutoExport_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempExportDir);
                string stageT6 = Path.Combine(tempExportDir, "stage_t6");
                Directory.CreateDirectory(stageT6);

                lblStatus.ForeColor = AccentOrange;
                lblStatus.Text = IsEn
                    ? "Packaging profile '" + prof.Name + "' into standalone executable..."
                    : "Empacotando perfil '" + prof.Name + "' em execut\u00e1vel \u00fanico...";
                progressBar.Value = 25;
                AppendLog("=== Exportando perfil como .exe: " + outExePath + " ===");

                for (int i = 0; i < prof.Items.Count; i++)
                {
                    ModEntryItem item = prof.Items[i];
                    string itemVaultDir = Path.Combine(profilesVaultDir, prof.Id, item.VaultSubFolder);
                    if (!Directory.Exists(itemVaultDir)) continue;

                    string targetStageDir = ResolveTargetBaseDir(stageT6, prof, item, itemVaultDir);
                    CopyDirectory(itemVaultDir, targetStageDir);
                }

                string payloadZip = Path.Combine(tempExportDir, "pack_payload.zip");
                ZipFile.CreateFromDirectory(stageT6, payloadZip, CompressionLevel.NoCompression, false);

                progressBar.Value = 65;
                Application.DoEvents();

                string templateCode;
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream st = asm.GetManifestResourceStream("InstallerTemplate.cs"))
                {
                    if (st == null)
                        throw new Exception("Modelo interno do instalador n\u00e3o encontrado.");
                    using (StreamReader sr = new StreamReader(st, Encoding.ASCII))
                    {
                        templateCode = sr.ReadToEnd();
                    }
                }

                string summary = prof.Items.Count + " mod(s)";
                templateCode = templateCode.Replace("__PACK_NAME__", EscapeCSharpString(prof.Name));
                templateCode = templateCode.Replace("__PACK_SUMMARY__", EscapeCSharpString(summary));

                string csFile = Path.Combine(tempExportDir, "StandaloneInstaller.cs");
                File.WriteAllText(csFile, templateCode, Encoding.ASCII);

                if (File.Exists(outExePath)) File.Delete(outExePath);

                string q = "\"";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cscPath;
                psi.Arguments = "/nologo /target:winexe /optimize+ /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /resource:" + q + payloadZip + q + ",pack_payload.zip /out:" + q + outExePath + q + " " + q + csFile + q;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process proc = Process.Start(psi);
                string stdOut = proc.StandardOutput.ReadToEnd();
                string stdErr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && File.Exists(outExePath))
                {
                    progressBar.Value = 100;
                    lblStatus.ForeColor = AccentGreen;
                    lblStatus.Text = (IsEn ? "Installer exported successfully: " : "Instalador exportado com sucesso: ") + Path.GetFileName(outExePath);
                    AppendLog("Execut\u00e1vel \u00fanico gerado com sucesso!");

                    DialogResult dr = MessageBox.Show(
                        IsEn
                            ? "Standalone installer created successfully!\n\nReady to share:\n" + outExePath + "\n\nOpen the folder where the .exe was saved?"
                            : "Instalador \u00fanico gerado com sucesso!\n\nArquivo pronto para distribuir:\n" + outExePath + "\n\nDeseja abrir a pasta onde o .exe foi salvo?",
                        IsEn ? "Export Complete" : "Exporta\u00e7\u00e3o Conclu\u00edda",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    );
                    if (dr == DialogResult.Yes)
                        Process.Start("explorer.exe", "/select," + q + outExePath + q);
                }
                else
                {
                    throw new Exception("Falha na compila\u00e7\u00e3o:\n" + stdOut + "\n" + stdErr);
                }
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = AccentCoral;
                lblStatus.Text = IsEn ? "Error exporting executable." : "Erro ao exportar execut\u00e1vel.";
                AppendLog("[ERRO] " + ex.Message);
                MessageBox.Show((IsEn ? "Error exporting .exe:\n" : "Erro ao exportar .exe:\n") + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                try { if (Directory.Exists(tempExportDir)) Directory.Delete(tempExportDir, true); } catch { }
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnActivateProfile.Enabled = enabled;
            btnDeactivateAll.Enabled = enabled;
            btnExportExe.Enabled = enabled;
            btnAddFiles.Enabled = enabled;
            btnAddFolder.Enabled = enabled;
            btnChangeDest.Enabled = enabled;
            btnRemoveItem.Enabled = enabled;
            btnNewProfile.Enabled = enabled;
            btnRenameProfile.Enabled = enabled;
            btnDuplicateProfile.Enabled = enabled;
            btnDeleteProfile.Enabled = enabled;
            btnCheckSync.Enabled = enabled;
            if (btnLangToggle != null) btnLangToggle.Enabled = enabled;
        }

        private static bool IsAssetFolder(string name)
        {
            string lower = name.ToLowerInvariant();
            for (int i = 0; i < AssetFolderNames.Length; i++)
            {
                if (AssetFolderNames[i] == lower) return true;
            }
            return false;
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            string[] files = Directory.GetFiles(sourceDir);
            for (int i = 0; i < files.Length; i++)
            {
                File.Copy(files[i], Path.Combine(destDir, Path.GetFileName(files[i])), true);
            }
            string[] dirs = Directory.GetDirectories(sourceDir);
            for (int i = 0; i < dirs.Length; i++)
            {
                CopyDirectory(dirs[i], Path.Combine(destDir, Path.GetFileName(dirs[i])));
            }
        }

        private static string GetRelativePath(string baseDir, string fullPath)
        {
            string b = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(b, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(b.Length);
            return Path.GetFileName(fullPath);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "ModPack";
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                name = name.Replace(invalid[i], '_');
            return name.Trim().Replace(' ', '_');
        }

        private static string EscapeCSharpString(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '\\') sb.Append("\\\\");
                else if (ch == '"') sb.Append("\\\"");
                else if (ch >= 32 && ch < 127) sb.Append(ch);
                else sb.AppendFormat("\\u{0:x4}", (int)ch);
            }
            return sb.ToString();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.0") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("0.0") + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("0.00") + " GB";
        }

        private string PromptInput(string title, string promptText, string defaultVal)
        {
            Form prompt = new Form();
            prompt.Width = 420;
            prompt.Height = 175;
            prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
            prompt.Text = title;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.MaximizeBox = false;
            prompt.MinimizeBox = false;
            prompt.BackColor = BgCard;
            prompt.ForeColor = TextLight;
            prompt.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            Label textLabel = new Label() { Left = 18, Top = 16, Width = 370, Text = promptText };
            TextBox textBox = new TextBox()
            {
                Left = 18,
                Top = 44,
                Width = 368,
                Text = defaultVal,
                BackColor = BgInput,
                ForeColor = TextLight,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button confirmation = CreateStyledButton(IsEn ? "Confirm" : "Confirmar", 200, 86, 90, 32, AccentOrange, Color.White);
            confirmation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            confirmation.FlatAppearance.BorderSize = 0;
            confirmation.DialogResult = DialogResult.OK;

            Button cancel = CreateStyledButton(IsEn ? "Cancel" : "Cancelar", 296, 86, 90, 32, BgInput, TextLight);
            cancel.DialogResult = DialogResult.Cancel;

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;
            prompt.Shown += (s, e) => { textBox.Focus(); textBox.SelectAll(); };

            return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
        }

        private void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            if (availableUpdateInfo != null)
            {
                OpenUpdateDialog(availableUpdateInfo);
                return;
            }
            StartAutomaticUpdateCheck(false);
        }

        private void StartAutomaticUpdateCheck(bool silentMode)
        {
            if (isCheckingUpdate) return;
            isCheckingUpdate = true;

            if (!silentMode && btnCheckUpdate != null)
            {
                btnCheckUpdate.Enabled = false;
                btnCheckUpdate.Text = IsEn ? "Checking..." : "Verificando...";
                lblStatus.ForeColor = AccentCyan;
                lblStatus.Text = IsEn
                    ? "Checking GitHub (NicolasPaiao2) for updates..."
                    : "Verificando atualiza\u00e7\u00f5es no GitHub (NicolasPaiao2)...";
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                if (silentMode)
                {
                    Thread.Sleep(1100);
                }
                e.Result = GitHubUpdateService.CheckForUpdates(GitHubOwner, GitHubRepoName, CurrentAppVersion);
            };
            bw.RunWorkerCompleted += (s, e) =>
            {
                isCheckingUpdate = false;
                if (btnCheckUpdate != null && !btnCheckUpdate.IsDisposed)
                {
                    btnCheckUpdate.Enabled = true;
                }

                if (e.Error != null)
                {
                    if (btnCheckUpdate != null && !btnCheckUpdate.IsDisposed)
                        btnCheckUpdate.Text = L("update_btn");

                    if (!silentMode)
                    {
                        lblStatus.ForeColor = AccentCoral;
                        lblStatus.Text = IsEn
                            ? "Could not check GitHub updates right now."
                            : "N\u00e3o foi poss\u00edvel verificar atualiza\u00e7\u00f5es no GitHub agora.";
                        AppendLog("[UPDATE] " + e.Error.Message);
                        DialogResult dr = MessageBox.Show(
                            this,
                            (IsEn
                                ? "Could not check for updates on GitHub:\n" + e.Error.Message + "\n\nOpen the GitHub releases page in your browser?"
                                : "N\u00e3o foi poss\u00edvel verificar atualiza\u00e7\u00f5es no GitHub:\n" + e.Error.Message + "\n\nDeseja abrir a p\u00e1gina do GitHub no navegador?"),
                            IsEn ? "Update Check" : "Verifica\u00e7\u00e3o de Atualiza\u00e7\u00e3o",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                        if (dr == DialogResult.Yes)
                        {
                            try { Process.Start(new ProcessStartInfo(GitHubRepoUrl + "/releases") { UseShellExecute = true }); } catch { }
                        }
                    }
                    return;
                }

                UpdateCheckResult res = e.Result as UpdateCheckResult;
                if (res == null)
                {
                    if (btnCheckUpdate != null && !btnCheckUpdate.IsDisposed)
                        btnCheckUpdate.Text = L("update_btn");
                    return;
                }

                if (res.HasUpdate && res.Release != null)
                {
                    availableUpdateInfo = res.Release;
                    if (btnCheckUpdate != null && !btnCheckUpdate.IsDisposed)
                    {
                        btnCheckUpdate.BackColor = AccentOrange;
                        btnCheckUpdate.ForeColor = Color.White;
                        btnCheckUpdate.Text = L("update_btn");
                    }

                    lblStatus.ForeColor = AccentOrange;
                    lblStatus.Text = IsEn
                        ? "New update available on GitHub: " + res.Release.TagName + " (current: v" + CurrentAppVersion + ")!"
                        : "Nova atualiza\u00e7\u00e3o dispon\u00edvel no GitHub: " + res.Release.TagName + " (atual: v" + CurrentAppVersion + ")!";
                    AppendLog(IsEn
                        ? "[UPDATE] New version found on GitHub: " + res.Release.TagName + " (installed: v" + CurrentAppVersion + ")"
                        : "[UPDATE] Nova vers\u00e3o encontrada no GitHub: " + res.Release.TagName + " (instalada: v" + CurrentAppVersion + ")");

                    OpenUpdateDialog(res.Release);
                }
                else
                {
                    if (btnCheckUpdate != null && !btnCheckUpdate.IsDisposed)
                        btnCheckUpdate.Text = L("update_btn");

                    if (!silentMode)
                    {
                        string remoteTagText = string.IsNullOrEmpty(res.LatestTagSeen) ? ("v" + CurrentAppVersion) : res.LatestTagSeen;
                        lblStatus.ForeColor = AccentGreen;
                        lblStatus.Text = IsEn
                            ? "You are using the latest version (v" + CurrentAppVersion + ")."
                            : "Voc\u00ea j\u00e1 est\u00e1 usando a vers\u00e3o mais recente (v" + CurrentAppVersion + ").";
                        AppendLog(IsEn
                            ? "[UPDATE] Up to date (v" + CurrentAppVersion + ", GitHub: " + remoteTagText + ")."
                            : "[UPDATE] Programa atualizado (v" + CurrentAppVersion + ", GitHub: " + remoteTagText + ").");

                        MessageBox.Show(
                            this,
                            (IsEn
                                ? "Your Plutonium T6 Mod & Pack Manager is up to date!\n\nInstalled version: v" + CurrentAppVersion + "\nLatest on GitHub (NicolasPaiao2): " + remoteTagText
                                : "Seu Plutonium T6 Mod & Pack Manager j\u00e1 est\u00e1 atualizado!\n\nVers\u00e3o instalada: v" + CurrentAppVersion + "\nMais recente no GitHub (NicolasPaiao2): " + remoteTagText),
                            IsEn ? "Up to Date" : "Programa Atualizado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            };
            bw.RunWorkerAsync();
        }

        private void OpenUpdateDialog(UpdateReleaseInfo release)
        {
            if (release == null) return;
            using (UpdateDialogForm dlg = new UpdateDialogForm(release, CurrentAppVersion, GitHubRepoUrl, IsEn))
            {
                dlg.ShowDialog(this);
            }
        }
    }

    [DataContract]
    public class GitHubAssetDto
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "browser_download_url")] public string BrowserDownloadUrl { get; set; }
        [DataMember(Name = "size")] public long Size { get; set; }
    }

    [DataContract]
    public class GitHubReleaseDto
    {
        [DataMember(Name = "tag_name")] public string TagName { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "body")] public string Body { get; set; }
        [DataMember(Name = "html_url")] public string HtmlUrl { get; set; }
        [DataMember(Name = "zipball_url")] public string ZipballUrl { get; set; }
        [DataMember(Name = "draft")] public bool Draft { get; set; }
        [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
        [DataMember(Name = "assets")] public List<GitHubAssetDto> Assets { get; set; }
    }

    [DataContract]
    public class GitHubTagDto
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "zipball_url")] public string ZipballUrl { get; set; }
    }

    public class UpdateReleaseInfo
    {
        public string TagName { get; set; }
        public string NormalizedVersion { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string HtmlUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string AssetFileName { get; set; }
        public long AssetSize { get; set; }
        public bool IsZipPackage { get; set; }
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string LatestTagSeen { get; set; }
        public UpdateReleaseInfo Release { get; set; }
    }

    public static class GitHubUpdateService
    {
        public static void CleanupOldUpdateArtifacts()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    string oldExe = exePath + ".old";
                    if (File.Exists(oldExe))
                    {
                        try { File.Delete(oldExe); } catch { }
                    }
                }
            }
            catch { }

            try
            {
                string tempRoot = Path.GetTempPath();
                string[] dirs = Directory.GetDirectories(tempRoot, "PlutoT6Update_*");
                for (int i = 0; i < dirs.Length; i++)
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(dirs[i]);
                        if ((DateTime.Now - di.CreationTime).TotalMinutes > 10)
                        {
                            Directory.Delete(dirs[i], true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void ConfigureTlsAndSecurity()
        {
            try
            {
                ServicePointManager.Expect100Continue = false;
                // 3072 = Tls12, 768 = Tls11 (numeric values required for .NET Framework 4.0 csc.exe compatibility)
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
            }
            catch { }
        }

        private static string HttpGetJson(string url, int timeoutMs)
        {
            ConfigureTlsAndSecurity();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "PlutoniumT6ModManager-Updater/4.5.1";
            req.Accept = "application/vnd.github.v3+json";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.AllowAutoRedirect = true;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream st = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        public static UpdateCheckResult CheckForUpdates(string owner, string repo, string currentVersion)
        {
            UpdateCheckResult result = new UpdateCheckResult();
            result.HasUpdate = false;
            result.LatestTagSeen = "v" + currentVersion;

            string baseApi = "https://api.github.com/repos/" + owner + "/" + repo;
            GitHubReleaseDto selectedRelease = null;

            // 1. Try /releases/latest first
            try
            {
                string json = HttpGetJson(baseApi + "/releases/latest", 12000);
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GitHubReleaseDto));
                    selectedRelease = (GitHubReleaseDto)ser.ReadObject(ms);
                }
            }
            catch (WebException wex)
            {
                HttpWebResponse errResp = wex.Response as HttpWebResponse;
                if (errResp == null || errResp.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            // 2. Fallback to /releases list (handles pre-releases or cases where /latest returns 404)
            if (selectedRelease == null)
            {
                try
                {
                    string jsonList = HttpGetJson(baseApi + "/releases?per_page=5", 12000);
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonList)))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<GitHubReleaseDto>));
                        List<GitHubReleaseDto> list = (List<GitHubReleaseDto>)ser.ReadObject(ms);
                        if (list != null)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] != null && !list[i].Draft && !string.IsNullOrEmpty(list[i].TagName))
                                {
                                    selectedRelease = list[i];
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (selectedRelease != null && !string.IsNullOrEmpty(selectedRelease.TagName))
            {
                result.LatestTagSeen = selectedRelease.TagName;
                string normRemote = ExtractVersionNumbers(selectedRelease.TagName);
                if (CompareVersions(normRemote, currentVersion) > 0)
                {
                    UpdateReleaseInfo info = BuildReleaseInfoFromDto(selectedRelease, normRemote, owner, repo);
                    if (info != null && !string.IsNullOrEmpty(info.DownloadUrl))
                    {
                        result.HasUpdate = true;
                        result.Release = info;
                    }
                }
                return result;
            }

            // 3. Fallback to /tags if repository only has Git tags without formal Release objects
            try
            {
                string tagsJson = HttpGetJson(baseApi + "/tags?per_page=5", 12000);
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(tagsJson)))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<GitHubTagDto>));
                    List<GitHubTagDto> tags = (List<GitHubTagDto>)ser.ReadObject(ms);
                    if (tags != null && tags.Count > 0 && !string.IsNullOrEmpty(tags[0].Name))
                    {
                        GitHubTagDto topTag = tags[0];
                        result.LatestTagSeen = topTag.Name;
                        string normTag = ExtractVersionNumbers(topTag.Name);
                        if (CompareVersions(normTag, currentVersion) > 0 && !string.IsNullOrEmpty(topTag.ZipballUrl))
                        {
                            UpdateReleaseInfo tagInfo = new UpdateReleaseInfo();
                            tagInfo.TagName = topTag.Name;
                            tagInfo.NormalizedVersion = normTag;
                            tagInfo.Title = "Release " + topTag.Name;
                            tagInfo.Notes = "";
                            tagInfo.HtmlUrl = "https://github.com/" + owner + "/" + repo + "/releases/tag/" + topTag.Name;
                            tagInfo.DownloadUrl = topTag.ZipballUrl;
                            tagInfo.AssetFileName = "source_" + topTag.Name + ".zip";
                            tagInfo.AssetSize = 0;
                            tagInfo.IsZipPackage = true;

                            result.HasUpdate = true;
                            result.Release = tagInfo;
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        private static UpdateReleaseInfo BuildReleaseInfoFromDto(GitHubReleaseDto dto, string normVersion, string owner, string repo)
        {
            UpdateReleaseInfo info = new UpdateReleaseInfo();
            info.TagName = dto.TagName;
            info.NormalizedVersion = normVersion;
            info.Title = string.IsNullOrWhiteSpace(dto.Name) ? dto.TagName : dto.Name.Trim();
            info.Notes = dto.Body ?? "";
            info.HtmlUrl = string.IsNullOrWhiteSpace(dto.HtmlUrl)
                ? ("https://github.com/" + owner + "/" + repo + "/releases")
                : dto.HtmlUrl;

            GitHubAssetDto chosenExe = null;
            GitHubAssetDto chosenZip = null;

            if (dto.Assets != null)
            {
                for (int i = 0; i < dto.Assets.Count; i++)
                {
                    GitHubAssetDto a = dto.Assets[i];
                    if (a == null || string.IsNullOrEmpty(a.Name) || string.IsNullOrEmpty(a.BrowserDownloadUrl))
                        continue;

                    string lower = a.Name.ToLowerInvariant();
                    if (lower.EndsWith(".exe") && !lower.StartsWith("instalador_"))
                    {
                        if (chosenExe == null || lower.Contains("manager") || lower.Contains("plutonium"))
                        {
                            chosenExe = a;
                        }
                    }
                    else if (lower.EndsWith(".zip"))
                    {
                        if (chosenZip == null || lower.Contains("manager") || lower.Contains("plutonium"))
                        {
                            chosenZip = a;
                        }
                    }
                }
            }

            if (chosenExe != null)
            {
                info.DownloadUrl = chosenExe.BrowserDownloadUrl;
                info.AssetFileName = chosenExe.Name;
                info.AssetSize = chosenExe.Size;
                info.IsZipPackage = false;
            }
            else if (chosenZip != null)
            {
                info.DownloadUrl = chosenZip.BrowserDownloadUrl;
                info.AssetFileName = chosenZip.Name;
                info.AssetSize = chosenZip.Size;
                info.IsZipPackage = true;
            }
            else if (!string.IsNullOrEmpty(dto.ZipballUrl))
            {
                info.DownloadUrl = dto.ZipballUrl;
                info.AssetFileName = "release_" + dto.TagName + ".zip";
                info.AssetSize = 0;
                info.IsZipPackage = true;
            }

            return info;
        }

        public static string ExtractVersionNumbers(string rawTag)
        {
            if (string.IsNullOrWhiteSpace(rawTag)) return "0.0.0";
            StringBuilder sb = new StringBuilder();
            bool started = false;
            for (int i = 0; i < rawTag.Length; i++)
            {
                char c = rawTag[i];
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                    started = true;
                }
                else if (c == '.' && started)
                {
                    sb.Append(c);
                }
                else if (started)
                {
                    break;
                }
            }
            string res = sb.ToString().Trim('.');
            return string.IsNullOrEmpty(res) ? "0.0.0" : res;
        }

        public static int CompareVersions(string remoteVer, string localVer)
        {
            int[] rParts = ParseVersionSegments(ExtractVersionNumbers(remoteVer));
            int[] lParts = ParseVersionSegments(ExtractVersionNumbers(localVer));
            int maxLen = Math.Max(rParts.Length, lParts.Length);
            for (int i = 0; i < maxLen; i++)
            {
                int rv = (i < rParts.Length) ? rParts[i] : 0;
                int lv = (i < lParts.Length) ? lParts[i] : 0;
                if (rv > lv) return 1;
                if (rv < lv) return -1;
            }
            return 0;
        }

        private static int[] ParseVersionSegments(string ver)
        {
            string[] tokens = ver.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            int[] nums = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                int val = 0;
                int.TryParse(tokens[i], out val);
                nums[i] = val;
            }
            return nums;
        }

        public static void DownloadStageAndApplyUpdate(
            UpdateReleaseInfo release,
            bool isEn,
            Action<int, string> reportProgress)
        {
            string tempUpdateDir = Path.Combine(Path.GetTempPath(), "PlutoT6Update_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempUpdateDir);

            string downloadTarget = Path.Combine(tempUpdateDir, release.IsZipPackage ? "release_pkg.zip" : "downloaded_app.exe");
            string stagedNewExe = Path.Combine(tempUpdateDir, "Plutonium_T6_ModManager_new.exe");
            string syncSourceDir = Path.Combine(tempUpdateDir, "sync_source");

            reportProgress(5, isEn ? "Connecting to GitHub..." : "Conectando ao GitHub...");
            DownloadFileWithProgress(release.DownloadUrl, downloadTarget, release.AssetSize, isEn, reportProgress);

            if (!release.IsZipPackage)
            {
                File.Copy(downloadTarget, stagedNewExe, true);
            }
            else
            {
                reportProgress(78, isEn ? "Extracting release package..." : "Extraindo pacote da atualiza\u00e7\u00e3o...");
                string extractDir = Path.Combine(tempUpdateDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(downloadTarget, extractDir);

                // Search for pre-compiled .exe inside extracted archive first
                string foundExe = FindPrebuiltExecutable(extractDir);
                string foundCs = FindSourceFile(extractDir, "PlutoniumModManager.cs");

                if (!string.IsNullOrEmpty(foundCs))
                {
                    string csFolder = Path.GetDirectoryName(foundCs);
                    if (Directory.Exists(csFolder))
                    {
                        CopyFolderSimple(csFolder, syncSourceDir);
                    }
                }

                if (!string.IsNullOrEmpty(foundExe) && IsValidWindowsExecutable(foundExe))
                {
                    reportProgress(88, isEn ? "Found updated executable in package..." : "Execut\u00e1vel atualizado encontrado no pacote...");
                    File.Copy(foundExe, stagedNewExe, true);
                }
                else if (!string.IsNullOrEmpty(foundCs))
                {
                    reportProgress(84, isEn ? "Compiling updated version with .NET Framework..." : "Compilando nova vers\u00e3o via .NET Framework...");
                    CompileUpdatedExecutableFromSource(foundCs, stagedNewExe, isEn);
                }
                else
                {
                    throw new Exception(isEn
                        ? "The downloaded GitHub archive does not contain an executable or PlutoniumModManager.cs source file."
                        : "O pacote baixado do GitHub n\u00e3o cont\u00e9m um execut\u00e1vel nem o c\u00f3digo-fonte PlutoniumModManager.cs.");
                }
            }

            reportProgress(94, isEn ? "Verifying executable integrity..." : "Verificando integridade do novo execut\u00e1vel...");
            if (!IsValidWindowsExecutable(stagedNewExe))
            {
                throw new Exception(isEn
                    ? "Downloaded update file failed integrity check (invalid or incomplete executable)."
                    : "O arquivo de atualiza\u00e7\u00e3o falhou na verifica\u00e7\u00e3o de integridade (execut\u00e1vel inv\u00e1lido ou incompleto).");
            }

            reportProgress(98, isEn ? "Applying update and restarting..." : "Aplicando atualiza\u00e7\u00e3o e reiniciando...");
            LaunchDetachedUpdaterAndExit(stagedNewExe, tempUpdateDir, Directory.Exists(syncSourceDir) ? syncSourceDir : null);
        }

        private static void DownloadFileWithProgress(
            string url,
            string destPath,
            long expectedSize,
            bool isEn,
            Action<int, string> reportProgress)
        {
            ConfigureTlsAndSecurity();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "PlutoniumT6ModManager-Updater/4.5.1";
            req.Accept = "application/octet-stream, application/vnd.github.v3+json, */*";
            req.Timeout = 20000;
            req.ReadWriteTimeout = 120000;
            req.AllowAutoRedirect = true;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                long totalBytes = resp.ContentLength > 0 ? resp.ContentLength : expectedSize;
                using (Stream inStream = resp.GetResponseStream())
                using (FileStream outStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[16384];
                    long totalRead = 0;
                    int bytesRead;
                    int lastReportedPct = 5;

                    while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outStream.Write(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            int pct = 10 + (int)((totalRead * 65) / totalBytes);
                            if (pct > 75) pct = 75;
                            if (pct > lastReportedPct)
                            {
                                lastReportedPct = pct;
                                reportProgress(pct, (isEn ? "Downloading update... " : "Baixando atualiza\u00e7\u00e3o... ") + pct + "%");
                            }
                        }
                        else
                        {
                            double kb = totalRead / 1024.0;
                            reportProgress(40, (isEn ? "Downloading update... " : "Baixando atualiza\u00e7\u00e3o... ") + kb.ToString("0") + " KB");
                        }
                    }
                }
            }
        }

        private static string FindPrebuiltExecutable(string rootDir)
        {
            string[] exes = Directory.GetFiles(rootDir, "*.exe", SearchOption.AllDirectories);
            string fallbackExe = null;
            for (int i = 0; i < exes.Length; i++)
            {
                string name = Path.GetFileName(exes[i]).ToLowerInvariant();
                if (name.StartsWith("instalador_")) continue;
                if (name.Contains("plutonium") || name.Contains("manager"))
                    return exes[i];
                if (fallbackExe == null)
                    fallbackExe = exes[i];
            }
            return fallbackExe;
        }

        private static string FindSourceFile(string rootDir, string targetFileName)
        {
            string[] files = Directory.GetFiles(rootDir, targetFileName, SearchOption.AllDirectories);
            if (files.Length > 0) return files[0];
            return null;
        }

        private static void CompileUpdatedExecutableFromSource(string csFilePath, string outExePath, bool isEn)
        {
            string csc64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
            string csc32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework", "v4.0.30319", "csc.exe");
            string cscPath = File.Exists(csc64) ? csc64 : (File.Exists(csc32) ? csc32 : null);

            if (cscPath == null)
            {
                throw new Exception(isEn
                    ? ".NET Framework compiler (csc.exe) not found to build source update."
                    : "Compilador .NET Framework (csc.exe) n\u00e3o encontrado para compilar a atualiza\u00e7\u00e3o.");
            }

            string sourceDir = Path.GetDirectoryName(csFilePath);
            string templatePath = Path.Combine(sourceDir, "StandaloneInstallerTemplate.cs");
            if (!File.Exists(templatePath))
            {
                // Extract embedded InstallerTemplate.cs from current running assembly as fallback
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream st = asm.GetManifestResourceStream("InstallerTemplate.cs"))
                {
                    if (st != null)
                    {
                        templatePath = Path.Combine(sourceDir, "StandaloneInstallerTemplate.cs");
                        using (FileStream fs = new FileStream(templatePath, FileMode.Create, FileAccess.Write))
                        {
                            st.CopyTo(fs);
                        }
                    }
                }
            }

            string iconPath = Path.Combine(sourceDir, "app.ico");
            if (!File.Exists(iconPath))
            {
                string currentDirIcon = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "app.ico");
                if (File.Exists(currentDirIcon))
                    iconPath = currentDirIcon;
                else
                    iconPath = null;
            }

            string q = "\"";
            StringBuilder args = new StringBuilder();
            args.Append("/nologo /target:winexe /optimize+ ");
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                args.Append("/win32icon:" + q + iconPath + q + " /resource:" + q + iconPath + q + ",AppIcon.ico ");
            }
            args.Append("/r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Runtime.Serialization.dll ");
            if (File.Exists(templatePath))
            {
                args.Append("/resource:" + q + templatePath + q + ",InstallerTemplate.cs ");
            }
            args.Append("/out:" + q + outExePath + q + " " + q + csFilePath + q);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = cscPath;
            psi.Arguments = args.ToString();
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process proc = Process.Start(psi);
            string stdOut = proc.StandardOutput.ReadToEnd();
            string stdErr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0 || !File.Exists(outExePath))
            {
                throw new Exception((isEn ? "Failed to compile update:\n" : "Falha ao compilar atualiza\u00e7\u00e3o:\n") + stdOut + "\n" + stdErr);
            }
        }

        private static bool IsValidWindowsExecutable(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length < 51200) return false; // Minimum 50 KB sanity check

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int b0 = fs.ReadByte();
                    int b1 = fs.ReadByte();
                    return (b0 == 0x4D && b1 == 0x5A); // 'M' 'Z' PE signature
                }
            }
            catch
            {
                return false;
            }
        }

        private static void CopyFolderSimple(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);
            string[] files = Directory.GetFiles(srcDir);
            for (int i = 0; i < files.Length; i++)
            {
                File.Copy(files[i], Path.Combine(dstDir, Path.GetFileName(files[i])), true);
            }
            string[] dirs = Directory.GetDirectories(srcDir);
            for (int i = 0; i < dirs.Length; i++)
            {
                CopyFolderSimple(dirs[i], Path.Combine(dstDir, Path.GetFileName(dirs[i])));
            }
        }

        private static void LaunchDetachedUpdaterAndExit(string newExePath, string tempUpdateDir, string syncSourceDir)
        {
            string targetExe = Application.ExecutablePath;
            string appDir = Path.GetDirectoryName(targetExe);
            int currentPid = Process.GetCurrentProcess().Id;
            string batPath = Path.Combine(tempUpdateDir, "apply_update.bat");

            bool shouldSyncSourceFiles = !string.IsNullOrEmpty(syncSourceDir)
                && Directory.Exists(syncSourceDir)
                && File.Exists(Path.Combine(appDir, "PlutoniumModManager.cs"));

            StringBuilder bat = new StringBuilder();
            bat.AppendLine("@echo off");
            bat.AppendLine("setlocal");
            bat.AppendLine("set \"PID=" + currentPid + "\"");
            bat.AppendLine("set \"TARGET_EXE=" + targetExe + "\"");
            bat.AppendLine("set \"NEW_EXE=" + newExePath + "\"");
            bat.AppendLine("set \"OLD_EXE=%TARGET_EXE%.old\"");
            bat.AppendLine("set \"APP_DIR=" + appDir + "\"");
            bat.AppendLine("set \"TEMP_DIR=" + tempUpdateDir + "\"");
            bat.AppendLine();
            bat.AppendLine("set /a COUNT=0");
            bat.AppendLine(":WAIT_LOOP");
            bat.AppendLine("tasklist /FI \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul");
            bat.AppendLine("if errorlevel 1 goto DO_REPLACE");
            bat.AppendLine("set /a COUNT+=1");
            bat.AppendLine("if %COUNT% GEQ 45 goto DO_REPLACE");
            bat.AppendLine("ping -n 2 127.0.0.1 >nul");
            bat.AppendLine("goto WAIT_LOOP");
            bat.AppendLine();
            bat.AppendLine(":DO_REPLACE");
            bat.AppendLine("ping -n 2 127.0.0.1 >nul");
            bat.AppendLine("if exist \"%OLD_EXE%\" del /f /q \"%OLD_EXE%\" >nul 2>&1");
            bat.AppendLine();
            bat.AppendLine("set /a RENAME_TRIES=0");
            bat.AppendLine(":RENAME_LOOP");
            bat.AppendLine("move /y \"%TARGET_EXE%\" \"%OLD_EXE%\" >nul 2>&1");
            bat.AppendLine("if not exist \"%TARGET_EXE%\" goto COPY_NEW");
            bat.AppendLine("set /a RENAME_TRIES+=1");
            bat.AppendLine("if %RENAME_TRIES% GEQ 15 goto FAIL_RESTORE");
            bat.AppendLine("ping -n 2 127.0.0.1 >nul");
            bat.AppendLine("goto RENAME_LOOP");
            bat.AppendLine();
            bat.AppendLine(":COPY_NEW");
            bat.AppendLine("copy /y \"%NEW_EXE%\" \"%TARGET_EXE%\" >nul 2>&1");
            bat.AppendLine("if exist \"%TARGET_EXE%\" goto SYNC_AND_LAUNCH");
            bat.AppendLine();
            bat.AppendLine(":FAIL_RESTORE");
            bat.AppendLine("if exist \"%OLD_EXE%\" move /y \"%OLD_EXE%\" \"%TARGET_EXE%\" >nul 2>&1");
            bat.AppendLine("goto CLEANUP");
            bat.AppendLine();
            bat.AppendLine(":SYNC_AND_LAUNCH");
            if (shouldSyncSourceFiles)
            {
                bat.AppendLine("xcopy /y /q \"" + syncSourceDir + "\\*.cs\" \"%APP_DIR%\\\" >nul 2>&1");
                bat.AppendLine("xcopy /y /q \"" + syncSourceDir + "\\*.bat\" \"%APP_DIR%\\\" >nul 2>&1");
                bat.AppendLine("xcopy /y /q \"" + syncSourceDir + "\\README.txt\" \"%APP_DIR%\\\" >nul 2>&1");
            }
            bat.AppendLine("start \"\" \"%TARGET_EXE%\"");
            bat.AppendLine();
            bat.AppendLine(":CLEANUP");
            bat.AppendLine("ping -n 3 127.0.0.1 >nul");
            bat.AppendLine("if exist \"%OLD_EXE%\" del /f /q \"%OLD_EXE%\" >nul 2>&1");
            bat.AppendLine("rmdir /s /q \"%TEMP_DIR%\" >nul 2>&1");
            bat.AppendLine("exit /b 0");

            File.WriteAllText(batPath, bat.ToString(), Encoding.ASCII);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c \"" + batPath + "\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.WorkingDirectory = tempUpdateDir;
            Process.Start(psi);

            Application.Exit();
            Environment.Exit(0);
        }
    }

    public class UpdateDialogForm : Form
    {
        private readonly UpdateReleaseInfo releaseInfo;
        private readonly string currentVersion;
        private readonly string repoUrl;
        private readonly bool isEn;

        private ProgressBar progressBar;
        private Label lblProgressStatus;
        private Button btnUpdateNow;
        private Button btnOpenGitHub;
        private Button btnLater;
        private bool isUpdating = false;

        public UpdateDialogForm(UpdateReleaseInfo release, string currentVer, string gitHubUrl, bool en)
        {
            this.releaseInfo = release;
            this.currentVersion = currentVer;
            this.repoUrl = gitHubUrl;
            this.isEn = en;

            Color bgPrimary = Color.FromArgb(20, 21, 25);
            Color bgCard = Color.FromArgb(29, 31, 38);
            Color bgInput = Color.FromArgb(37, 40, 50);
            Color borderSubtle = Color.FromArgb(52, 56, 70);
            Color accentOrange = Color.FromArgb(245, 130, 13);
            Color accentGreen = Color.FromArgb(46, 204, 113);
            Color accentCyan = Color.FromArgb(56, 189, 248);
            Color textLight = Color.FromArgb(243, 244, 246);
            Color textMuted = Color.FromArgb(156, 163, 175);

            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = en ? "Update Available - Plutonium T6 Mod Manager" : "Atualiza\u00e7\u00e3o Dispon\u00edvel - Plutonium T6 Mod Manager";
            this.Size = new Size(540, 410);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = bgPrimary;
            this.ForeColor = textLight;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 68;
            pnlHeader.BackColor = bgCard;

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 4;
            topBar.BackColor = accentOrange;
            pnlHeader.Controls.Add(topBar);

            Label lblTitle = new Label();
            lblTitle.Text = en ? "NEW VERSION AVAILABLE ON GITHUB" : "NOVA VERS\u00c3O DISPON\u00cdVEL NO GITHUB";
            lblTitle.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
            lblTitle.ForeColor = accentOrange;
            lblTitle.Location = new Point(18, 12);
            lblTitle.AutoSize = true;

            Label lblSub = new Label();
            lblSub.Text = (en ? "Installed: v" : "Instalada: v") + currentVer + "   ->   " + (en ? "Available: " : "Dispon\u00edvel: ") + release.TagName;
            lblSub.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSub.ForeColor = accentGreen;
            lblSub.Location = new Point(20, 39);
            lblSub.AutoSize = true;

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            this.Controls.Add(pnlHeader);

            Label lblNotesTitle = new Label();
            lblNotesTitle.Text = (en ? "Release Notes (" : "Notas da Vers\u00e3o (") + release.Title + "):";
            lblNotesTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblNotesTitle.ForeColor = textMuted;
            lblNotesTitle.Location = new Point(18, 80);
            lblNotesTitle.AutoSize = true;
            this.Controls.Add(lblNotesTitle);

            RichTextBox rtbNotes = new RichTextBox();
            rtbNotes.Location = new Point(18, 104);
            rtbNotes.Size = new Size(494, 152);
            rtbNotes.BackColor = bgInput;
            rtbNotes.ForeColor = textLight;
            rtbNotes.BorderStyle = BorderStyle.FixedSingle;
            rtbNotes.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            rtbNotes.ReadOnly = true;
            rtbNotes.Text = string.IsNullOrWhiteSpace(release.Notes)
                ? (en ? "Official update published on NicolasPaiao2 GitHub repository." : "Atualiza\u00e7\u00e3o oficial publicada no reposit\u00f3rio GitHub NicolasPaiao2.")
                : release.Notes.Trim();
            this.Controls.Add(rtbNotes);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(18, 268);
            progressBar.Size = new Size(494, 18);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            this.Controls.Add(progressBar);

            lblProgressStatus = new Label();
            string pkgType = release.IsZipPackage
                ? (en ? "Release Package (.zip)" : "Pacote de Atualiza\u00e7\u00e3o (.zip)")
                : (en ? "Direct Executable (.exe)" : "Execut\u00e1vel Direto (.exe)");
            lblProgressStatus.Text = (en ? "Ready to update via " : "Pronto para atualizar via ") + pkgType + ".";
            lblProgressStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblProgressStatus.ForeColor = accentCyan;
            lblProgressStatus.Location = new Point(18, 292);
            lblProgressStatus.Size = new Size(494, 18);
            this.Controls.Add(lblProgressStatus);

            btnUpdateNow = new Button();
            btnUpdateNow.Text = en ? "Update Now & Restart" : "Atualizar Agora e Reiniciar";
            btnUpdateNow.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnUpdateNow.Location = new Point(18, 322);
            btnUpdateNow.Size = new Size(218, 38);
            btnUpdateNow.FlatStyle = FlatStyle.Flat;
            btnUpdateNow.FlatAppearance.BorderSize = 0;
            btnUpdateNow.BackColor = accentGreen;
            btnUpdateNow.ForeColor = Color.FromArgb(15, 23, 18);
            btnUpdateNow.Cursor = Cursors.Hand;
            btnUpdateNow.Click += BtnUpdateNow_Click;
            this.Controls.Add(btnUpdateNow);

            btnOpenGitHub = new Button();
            btnOpenGitHub.Text = en ? "Open GitHub" : "Abrir no GitHub";
            btnOpenGitHub.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btnOpenGitHub.Location = new Point(246, 322);
            btnOpenGitHub.Size = new Size(134, 38);
            btnOpenGitHub.FlatStyle = FlatStyle.Flat;
            btnOpenGitHub.FlatAppearance.BorderColor = borderSubtle;
            btnOpenGitHub.FlatAppearance.BorderSize = 1;
            btnOpenGitHub.BackColor = bgInput;
            btnOpenGitHub.ForeColor = textLight;
            btnOpenGitHub.Cursor = Cursors.Hand;
            btnOpenGitHub.Click += (s, e) =>
            {
                try
                {
                    string targetUrl = !string.IsNullOrWhiteSpace(release.HtmlUrl) ? release.HtmlUrl : repoUrl;
                    Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
                }
                catch { }
            };
            this.Controls.Add(btnOpenGitHub);

            btnLater = new Button();
            btnLater.Text = en ? "Later" : "Depois";
            btnLater.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btnLater.Location = new Point(390, 322);
            btnLater.Size = new Size(122, 38);
            btnLater.FlatStyle = FlatStyle.Flat;
            btnLater.FlatAppearance.BorderColor = borderSubtle;
            btnLater.FlatAppearance.BorderSize = 1;
            btnLater.BackColor = bgInput;
            btnLater.ForeColor = textLight;
            btnLater.Cursor = Cursors.Hand;
            btnLater.Click += (s, e) => this.Close();
            this.Controls.Add(btnLater);

            this.FormClosing += (s, e) =>
            {
                if (isUpdating)
                {
                    e.Cancel = true;
                }
            };
        }

        private void BtnUpdateNow_Click(object sender, EventArgs e)
        {
            if (isUpdating) return;
            isUpdating = true;
            btnUpdateNow.Enabled = false;
            btnOpenGitHub.Enabled = false;
            btnLater.Enabled = false;

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += (s, args) =>
            {
                GitHubUpdateService.DownloadStageAndApplyUpdate(
                    releaseInfo,
                    isEn,
                    (pct, statusText) =>
                    {
                        bw.ReportProgress(pct, statusText);
                    }
                );
            };
            bw.ProgressChanged += (s, args) =>
            {
                int val = Math.Max(0, Math.Min(100, args.ProgressPercentage));
                progressBar.Value = val;
                if (args.UserState != null)
                {
                    lblProgressStatus.Text = args.UserState.ToString();
                }
            };
            bw.RunWorkerCompleted += (s, args) =>
            {
                isUpdating = false;
                if (args.Error != null)
                {
                    progressBar.Value = 0;
                    lblProgressStatus.ForeColor = Color.FromArgb(251, 113, 133);
                    lblProgressStatus.Text = isEn ? "Update failed." : "Falha na atualiza\u00e7\u00e3o.";
                    btnUpdateNow.Enabled = true;
                    btnOpenGitHub.Enabled = true;
                    btnLater.Enabled = true;
                    MessageBox.Show(
                        this,
                        (isEn
                            ? "Error applying automatic update:\n" + args.Error.Message + "\n\nYou can also download it manually from GitHub."
                            : "Erro ao aplicar atualiza\u00e7\u00e3o autom\u00e1tica:\n" + args.Error.Message + "\n\nVoc\u00ea tamb\u00e9m pode baixar manualmente pelo GitHub."),
                        isEn ? "Update Error" : "Erro na Atualiza\u00e7\u00e3o",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };
            bw.RunWorkerAsync();
        }
    }
}
