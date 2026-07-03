namespace WinterRose.Nexus.Interface;

using Gtk;
using Gdk;
using System;

public static class ThemeManager
{
    public static void ApplyNexusTheme()
    {
        CssProvider cssProvider = new CssProvider();

        cssProvider.LoadFromData("""
        /* =========================================================
           NEXUS COLOR VARIABLES
           GTK CSS doesn't read plain `--custom-properties`; it has its
           own variable system: @define-color NAME value; then @NAME.
           ========================================================= */

        @define-color nx_bg              #0e0612;
        @define-color nx_bg_elevated     #170c1d;
        @define-color nx_bg_card         #1c0f24;
        @define-color nx_border          #2d1640;
        @define-color nx_border_soft     #241433;
        @define-color nx_accent          #fcadff;
        @define-color nx_accent_dim      #c98ed1;
        @define-color nx_text            #ffffff;
        @define-color nx_text_muted      #b8a8c0;

        @define-color nx_danger          #ff5a6a;
        @define-color nx_danger_soft     rgba(255, 90, 106, 0.15);
        @define-color nx_danger_border   rgba(255, 90, 106, 0.4);
        @define-color nx_danger_text     #ffb3b3;

        /* =========================================================
           GLOBAL RESET
           This is the important part for the border problem: the
           default theme draws its 'borders' using box-shadow and
           background-image gradients layered on top of whatever
           background-color/border you set. We strip those globally
           first, then apply our own look on top.
           ========================================================= */

        *,
        *:hover,
        *:active,
        *:checked,
        *:selected,
        *:backdrop,
        *:disabled {
            background-image: none;
            box-shadow: none;
            text-shadow: none;
            border-image: none;
            -gtk-icon-shadow: none;
            outline-color: alpha(@nx_accent, 0);
        }

        * {
            background-color: @nx_bg;
            color: @nx_text;
            border-color: @nx_border;
            caret-color: @nx_accent;
        }

        window {
            background-color: @nx_bg;
        }

        /* =========================================================
           BASE LAYOUT CONTAINERS
           ========================================================= */

        box,
        grid,
        paned,
        flowbox {
            background-color: @nx_bg;
        }

        frame,
        viewport,
        scrolledwindow {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
            border-radius: 10px;
        }

        /* =========================================================
           WINDOWS / DIALOGS
           ========================================================= */

        dialog,
        messagedialog,
        window.dialog {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
        }

        dialog decoration,
        window decoration {
            background-color: @nx_bg;
            border-bottom: 1px solid @nx_border;
        }

        /* =========================================================
           CARDS
           ========================================================= */

        .card {
            background-color: @nx_bg_card;
            border: 1px solid @nx_border;
            border-radius: 12px;
            padding: 10px;
        }

        /* =========================================================
           LABELS / TEXT
           ========================================================= */

        label {
            color: @nx_text;
        }

        label.muted {
            color: @nx_text_muted;
        }

        .link {
            color: @nx_accent;
        }

        .link:hover {
            color: @nx_accent_dim;
        }

        /* =========================================================
           BUTTONS (FULL STATE SYSTEM)
           ========================================================= */

        button {
            background-color: @nx_bg_card;
            background-image: none;
            box-shadow: none;
            border: 1px solid @nx_border;
            color: @nx_text;
            border-radius: 8px;
            padding: 6px 10px;
            transition: 150ms ease;
        }

        /* Kill the inner content node's own background so it doesn't
           paint a separate-colored rectangle inside the button. GTK
           wraps button content in its own node that otherwise falls
           back to the theme default instead of inheriting from
           `button` above. */
        button > *,
        button label,
        button box,
        button image {
            background-color: transparent;
            background-image: none;
        }

        button:hover {
            background-color: @nx_border_soft;
            border-color: @nx_accent;
        }

        button:active,
        button:checked {
            background-color: @nx_bg_elevated;
            border-color: @nx_accent_dim;
        }

        button:disabled {
            opacity: 0.4;
        }

        button:backdrop {
            background-color: @nx_bg_card;
            border-color: @nx_border;
        }

        /* Primary */
        button.primary,
        button.suggested-action {
            background-color: @nx_accent;
            color: @nx_bg;
            border-color: @nx_accent_dim;
        }

        button.primary:hover,
        button.suggested-action:hover {
            background-color: @nx_accent_dim;
        }

        button.primary > *,
        button.suggested-action > * {
            background-color: transparent;
            color: @nx_bg;
        }

        /* Destructive */
        button.destructive-action {
            background-color: @nx_danger_soft;
            border: 1px solid @nx_danger_border;
            color: @nx_danger_text;
        }

        button.destructive-action:hover {
            background-color: alpha(@nx_danger, 0.25);
            border-color: @nx_danger;
        }

        button.destructive-action > * {
            background-color: transparent;
            color: @nx_danger_text;
        }

        /* Flat buttons */
        button.flat,
        button.flat:hover,
        button.flat:active {
            background-color: transparent;
            border: none;
            box-shadow: none;
        }

        /* Titlebar buttons (close/min/max) */
        headerbar button,
        .titlebutton {
            background-color: transparent;
            border: none;
            box-shadow: none;
        }

        headerbar button:hover,
        .titlebutton:hover {
            background-color: @nx_border_soft;
        }

        /* Spin buttons (up/down arrows) */
        spinbutton {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
            border-radius: 8px;
        }

        spinbutton button {
            background-color: transparent;
            border: none;
            box-shadow: none;
            border-left: 1px solid @nx_border;
            border-radius: 0;
        }

        spinbutton button:hover {
            background-color: @nx_border_soft;
        }

        /* =========================================================
           ENTRY / TEXT INPUTS
           ========================================================= */

        entry,
        searchentry,
        textview {
            background-color: @nx_bg_elevated;
            background-image: none;
            box-shadow: none;
            color: @nx_text;
            border: 1px solid @nx_border;
            border-radius: 8px;
            padding: 6px;
        }

        entry:focus,
        searchentry:focus,
        textview:focus {
            border-color: @nx_accent;
            box-shadow: none;
        }

        textview text {
            background-color: @nx_bg_elevated;
            color: @nx_text;
        }

        /* Selection (global text selection) */
        selection,
        *:selected {
            background-color: @nx_accent;
            color: @nx_bg;
        }

        /* =========================================================
           LIST / TREE / GRID
           ========================================================= */

        list,
        list row {
            background-color: transparent;
        }

        list row {
            padding: 6px;
            border-radius: 6px;
        }

        list row:hover {
            background-color: alpha(@nx_accent, 0.08);
        }

        list row:selected {
            background-color: @nx_border_soft;
            border-left: 3px solid @nx_accent;
        }

        treeview.view {
            background-color: @nx_bg_elevated;
            color: @nx_text;
        }

        treeview.view:selected {
            background-color: @nx_border_soft;
        }

        treeview.view header button {
            background-color: @nx_bg_card;
            border: 1px solid @nx_border;
            box-shadow: none;
        }

        /* =========================================================
           COMBOBOX / DROPDOWN
           (This is usually the other big source of "default colored
           borders" — the little arrow button inside the combo has its
           own box-shadow/border-image that needs resetting explicitly.)
           ========================================================= */

        combobox {
            background-color: transparent;
        }

        combobox box.linked button,
        combobox button,
        combobox button.combo,
        dropdown,
        dropdown button {
            background-color: @nx_bg_elevated;
            background-image: none;
            box-shadow: none;
            border: 1px solid @nx_border;
            color: @nx_text;
        }

        combobox button > *,
        dropdown button > * {
            background-color: transparent;
        }

        combobox button:hover,
        dropdown button:hover {
            background-color: @nx_border_soft;
            border-color: @nx_accent;
        }

        combobox arrow,
        dropdown arrow {
            color: @nx_text;
        }

        /* dropdown popover */
        popover {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
            box-shadow: none;
        }

        popover.background,
        popover contents {
            background-color: @nx_bg_elevated;
        }

        popover arrow {
            background-color: @nx_bg_elevated;
            border-color: @nx_border;
        }

        /* =========================================================
           CHECKBOX / RADIO
           ========================================================= */

        checkbutton,
        radiobutton {
            color: @nx_text;
        }

        checkbutton check,
        radiobutton radio {
            background-color: @nx_bg_elevated;
            background-image: none;
            box-shadow: none;
            border: 1px solid @nx_border;
            border-radius: 4px;
        }

        checkbutton check:checked,
        radiobutton radio:checked {
            background-color: @nx_accent;
            border-color: @nx_accent_dim;
        }

        /* =========================================================
           SWITCH
           ========================================================= */

        switch {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
            border-radius: 999px;
            box-shadow: none;
        }

        switch:checked {
            background-color: @nx_accent_dim;
            border-color: @nx_accent;
        }

        switch slider {
            background-color: @nx_text;
            border-radius: 999px;
            box-shadow: none;
        }

        /* =========================================================
           SLIDERS
           ========================================================= */

        scale trough {
            background-color: @nx_border;
        }

        scale highlight {
            background-color: @nx_accent;
        }

        scale slider {
            background-color: @nx_accent;
            border-radius: 50%;
            box-shadow: none;
        }

        /* =========================================================
           PROGRESS BAR / LEVEL BAR
           ========================================================= */

        progressbar trough,
        levelbar trough {
            background-color: @nx_bg_elevated;
            border-radius: 6px;
        }

        progressbar progress,
        levelbar block.filled {
            background-color: @nx_accent;
            border-radius: 6px;
        }

        /* =========================================================
           SCROLLBARS
           ========================================================= */

        scrollbar {
            background-color: transparent;
        }

        scrollbar slider {
            background-color: @nx_border;
            border-radius: 6px;
        }

        scrollbar slider:hover {
            background-color: @nx_accent;
        }

        /* =========================================================
           SEPARATORS / PANED HANDLE
           ========================================================= */

        separator {
            background-color: @nx_border;
            min-width: 1px;
            min-height: 1px;
        }

        paned separator,
        paned > separator {
            background-color: @nx_border;
        }

        /* =========================================================
           TABS
           ========================================================= */

        notebook header {
            background-color: @nx_bg;
        }

        tab {
            background-color: @nx_bg_elevated;
            background-image: none;
            box-shadow: none;
            border: 1px solid @nx_border;
            border-radius: 6px;
            padding: 6px 10px;
        }

        tab:checked {
            background-color: @nx_border_soft;
            border-color: @nx_accent;
        }

        /* =========================================================
           MENU BAR / MENUS
           ========================================================= */

        menubar {
            background-color: @nx_bg;
            border-bottom: 1px solid @nx_border;
        }

        menu,
        menuitem {
            background-color: @nx_bg_elevated;
            box-shadow: none;
        }

        menuitem:hover {
            background-color: @nx_border_soft;
        }

        /* =========================================================
           INFOBAR
           ========================================================= */

        infobar {
            background-color: @nx_bg_card;
            border: 1px solid @nx_border;
            color: @nx_text;
        }

        infobar.error {
            background-color: @nx_danger_soft;
            border-color: @nx_danger_border;
            color: @nx_danger_text;
        }

        /* =========================================================
           CALENDAR
           ========================================================= */

        calendar {
            background-color: @nx_bg_elevated;
            color: @nx_text;
            border: 1px solid @nx_border;
        }

        calendar:selected {
            background-color: @nx_accent;
            color: @nx_bg;
        }

        /* =========================================================
           TOOLTIP
           ========================================================= */

        tooltip {
            background-color: @nx_bg_elevated;
            border: 1px solid @nx_border;
            color: @nx_text;
            border-radius: 6px;
            box-shadow: none;
        }

        /* =========================================================
           HEADERBAR (VERY IMPORTANT FOR GTK APPS)
           ========================================================= */

        headerbar {
            background-color: @nx_bg;
            background-image: none;
            box-shadow: none;
            border-bottom: 1px solid @nx_border;
            color: @nx_text;
        }

        /* =========================================================
           FOCUS RING
           ========================================================= */

        *:focus {
            outline: 2px solid @nx_accent;
            outline-offset: 2px;
        }
        """);

        StyleContext.AddProviderForScreen(
            Screen.Default,
            cssProvider,
            uint.MaxValue
        );
    }
}