using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using GameConsole = global::Console;

namespace HammerHotkeys;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class HammerHotkeysPlugin : BaseUnityPlugin
{
    internal const string PluginGUID = "DMT.HammerHotkeys";
    internal const string PluginName = "HammerHotkeys";
    internal const string PluginVersion = "1.0.0";

    private const int MaxPendingAttempts = 30;

    private static ConfigEntry<string> BindsEntry = null;
    private static ConfigEntry<bool> AutoEquipEntry = null;
    private static ConfigEntry<bool> RequireAvailableEntry = null;
    private static ConfigEntry<bool> PlaySoundEntry = null;
    private static ConfigEntry<bool> ShowMessagesEntry = null;
    private static ConfigEntry<bool> OpenMenuEntry = null;

    private readonly List<Bind> _binds = new List<Bind>();
    private readonly HashSet<string> _warnedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private Piece _pending = null;
    private int _pendingAttempts;

    private struct Bind
    {
        public KeyCode Key;
        public string Piece;
    }

    private void Awake()
    {
        BindsEntry = Config.Bind("1 - General", "Binds", "F6=Workbench;F7=Portal;F8=Stonecutter",
            "Key=Piece pairs separated by ';'. The piece name is matched against the prefab name, " +
            "the $piece_... localization key, and the English display name.");
        AutoEquipEntry = Config.Bind("1 - General", "Auto Equip Hammer", true,
            "Equip the tool that contains the piece automatically if it is not already held.");
        RequireAvailableEntry = Config.Bind("1 - General", "Require Available", true,
            "Only select pieces that are currently available/unlocked (same rule as the vanilla build menu).");
        PlaySoundEntry = Config.Bind("1 - General", "Play Sound", true,
            "Play the vanilla selection sound when a piece is selected.");
        ShowMessagesEntry = Config.Bind("1 - General", "Show Messages", true,
            "Show a center screen message when a bind fires or fails.");
        OpenMenuEntry = Config.Bind("1 - General", "Open Build Menu On Select", false,
            "Open the build menu after selecting the piece instead of keeping it closed.");

        ParseBinds();
        BindsEntry.SettingChanged += OnBindsSettingChanged;

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded with {_binds.Count} bind(s)");
    }

    private void OnBindsSettingChanged(object sender, EventArgs e) => ParseBinds();

    private void ParseBinds()
    {
        _binds.Clear();

        string raw = BindsEntry.Value ?? string.Empty;
        foreach (string part in raw.Split(';'))
        {
            string entry = part.Trim();
            if (entry.Length == 0)
                continue;

            int eq = entry.IndexOf('=');
            if (eq <= 0 || eq >= entry.Length - 1)
            {
                Logger.LogWarning($"Invalid bind '{entry}', expected format Key=Piece");
                continue;
            }

            string keyText = entry.Substring(0, eq).Trim();
            string pieceName = entry.Substring(eq + 1).Trim();

            if (!Enum.TryParse(keyText, true, out KeyCode key))
            {
                Logger.LogWarning($"Invalid key '{keyText}' in bind '{entry}'");
                continue;
            }

            _binds.Add(new Bind { Key = key, Piece = pieceName });
        }

        Logger.LogInfo($"Loaded {_binds.Count} bind(s): {DescribeBinds()}");
    }

    private string DescribeBinds()
    {
        if (_binds.Count == 0)
            return "none";

        List<string> parts = new List<string>(_binds.Count);
        foreach (Bind bind in _binds)
            parts.Add($"{bind.Key}={bind.Piece}");
        return string.Join(", ", parts);
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null || IsBlocked(player))
            return;

        if (_pending != null)
            AttemptPending(player);

        if (_binds.Count == 0)
            return;

        foreach (Bind bind in _binds)
        {
            if (!Input.GetKeyDown(bind.Key))
                continue;
            HandleBind(player, bind);
            break;
        }
    }

    private static bool IsBlocked(Player player)
    {
        return player.IsDead()
            || GameConsole.IsVisible()
            || TextInput.IsVisible()
            || InventoryGui.IsVisible()
            || Menu.IsVisible();
    }

    private void HandleBind(Player player, Bind bind)
    {
        if (!TryResolve(player, bind.Piece, out ItemDrop.ItemData tool, out Piece piece))
        {
            WarnUnknownPiece(player, bind.Piece);
            return;
        }

        ItemDrop.ItemData right = player.RightItem;
        if (right != tool)
        {
            if (!AutoEquipEntry.Value)
            {
                ShowMessage(player, $"Hold {DescribeTool(tool)} first");
                return;
            }

            if (!player.EquipItem(tool, true))
            {
                ShowMessage(player, $"Can't equip {DescribeTool(tool)}");
                return;
            }
        }

        if (RequireAvailableEntry.Value && !player.IsPieceAvailable(piece))
        {
            ShowMessage(player, $"{DescribePiece(piece)} is not available yet");
            return;
        }

        _pending = piece;
        _pendingAttempts = MaxPendingAttempts;
        AttemptPending(player);
    }

    private void AttemptPending(Player player)
    {
        Piece piece = _pending;
        if (piece == null)
            return;

        bool selected = player.SetSelectedPiece(piece);
        if (selected || player.GetSelectedPiece() == piece)
        {
            _pending = null;
            OnSelected(player, piece);
            return;
        }

        if (--_pendingAttempts <= 0)
        {
            _pending = null;
            ShowMessage(player, $"Couldn't select {DescribePiece(piece)}");
            Logger.LogWarning($"SetSelectedPiece failed for '{piece.name}' (place mode not ready?)");
        }
    }

    private void OnSelected(Player player, Piece piece)
    {
        Hud hud = Hud.instance;
        BuildUi buildUi = hud != null ? hud.m_buildUi : null;
        bool menuOpen = buildUi != null && buildUi.gameObject.activeInHierarchy;

        if (OpenMenuEntry.Value)
        {
            if (buildUi != null && !menuOpen)
                buildUi.OpenBuildMenu();
        }
        else if (menuOpen)
        {
            buildUi.Close();
        }

        if (PlaySoundEntry.Value)
            player.PlayButtonSound();

        ShowMessage(player, $"{DescribePiece(piece)} ready");
    }

    private void ShowMessage(Player player, string text)
    {
        if (!ShowMessagesEntry.Value)
            return;

        player.Message(MessageHud.MessageType.Center, text);
    }

    private void WarnUnknownPiece(Player player, string name)
    {
        if (_warnedNames.Add(name))
            Logger.LogWarning($"No piece named '{name}' found in your build tools. " +
                              "Use the prefab name, the $piece_... localization key, or the English display name.");

        ShowMessage(player, $"Unknown piece '{name}'");
    }

    private static bool TryResolve(Player player, string name, out ItemDrop.ItemData tool, out Piece piece)
    {
        tool = null;
        piece = null;

        Inventory inventory = player.GetInventory();
        if (inventory == null)
            return false;

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (items == null)
            return false;

        ItemDrop.ItemData fallbackTool = null;
        Piece fallbackPiece = null;

        foreach (ItemDrop.ItemData item in items)
        {
            if (item == null || item.m_shared == null)
                continue;

            PieceTable table = item.m_shared.m_buildPieces;
            if (table == null || table.m_pieces == null)
                continue;

            Piece match = FindPiece(table, name);
            if (match == null)
                continue;

            if (IsBuildTool(item))
            {
                tool = item;
                piece = match;
                return true;
            }

            if (fallbackTool == null)
            {
                fallbackTool = item;
                fallbackPiece = match;
            }
        }

        tool = fallbackTool;
        piece = fallbackPiece;
        return piece != null;
    }

    private static bool IsBuildTool(ItemDrop.ItemData item)
    {
        if (item.m_dropPrefab != null &&
            item.m_dropPrefab.name.IndexOf("hammer", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return item.m_shared.m_name != null &&
               item.m_shared.m_name.IndexOf("hammer", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Piece FindPiece(PieceTable table, string name)
    {
        foreach (GameObject go in table.m_pieces)
        {
            if (go == null)
                continue;

            Piece piece = go.GetComponent<Piece>();
            if (piece != null && Matches(piece, go, name))
                return piece;
        }

        return null;
    }

    private static bool Matches(Piece piece, GameObject go, string name)
    {
        if (string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase))
            return true;

        string locKey = piece.m_name;
        if (string.IsNullOrEmpty(locKey))
            return false;

        if (string.Equals(locKey, name, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(locKey.TrimStart('$'), name, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(locKey, "$" + name, StringComparison.OrdinalIgnoreCase))
            return true;

        Localization localization = Localization.instance;
        if (localization == null)
            return false;

        string translated = localization.Localize(locKey);
        if (string.Equals(translated, name, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(translated, locKey, StringComparison.Ordinal))
            return false;

        string trimmed = locKey.TrimStart('$');
        translated = localization.Localize(trimmed);
        return string.Equals(translated, name, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribePiece(Piece piece)
    {
        if (piece == null)
            return string.Empty;

        string locKey = piece.m_name;
        Localization localization = Localization.instance;
        if (!string.IsNullOrEmpty(locKey) && localization != null)
        {
            string translated = localization.Localize(locKey);
            if (string.Equals(translated, locKey, StringComparison.Ordinal))
                translated = localization.Localize(locKey.TrimStart('$'));

            if (!string.IsNullOrEmpty(translated) && !string.Equals(translated, locKey, StringComparison.Ordinal))
                return translated;
        }

        return piece.name;
    }

    private static string DescribeTool(ItemDrop.ItemData item)
    {
        if (item == null)
            return "the tool";

        if (item.m_dropPrefab != null)
            return item.m_dropPrefab.name;

        return string.IsNullOrEmpty(item.m_shared?.m_name) ? "the tool" : item.m_shared.m_name;
    }
}
