using DSharpPlus.Entities;

namespace CraftBot
{
    public static class Emoji
    {
        public const string STATUS_OFFLINE = "<:offline:637413124550557709>";
        public const string STATUS_ONLINE = "<:online:637413124366139398>";
        public const string STATUS_IDLE = "<:idle:637413124193910811>";
        public const string STATUS_BUSY = "<:dnd:637413123992846359>";

        public const string STATUS_DESKTOP_ONLINE = "<:desktop_online:638943670653026304>";
        public const string STATUS_DESKTOP_IDLE = "<:desktop_idle:638943670254567425>";
        public const string STATUS_DESKTOP_BUSY = "<:desktop_busy:638943670661546014>";

        public const string STATUS_MOBILE_ONLINE = "<:mobile_online:638943670787244032>";
        public const string STATUS_MOBILE_IDLE = "<:mobile_idle:638943670476734495>";
        public const string STATUS_MOBILE_BUSY = "<:mobile_busy:638943670225207297>";

        public const string STATUS_WEB_ONLINE = "<:web_online:638943671454269450>";
        public const string STATUS_WEB_IDLE = "<:web_idle:638943670892232745>";
        public const string STATUS_WEB_BUSY = "<:web_busy:638943671017930753>";

        public const string ICON_CALENDAR_PLUS = "<:calendar_plus:534031917721452544>";
        public const string ICON_CALENDAR_UP = "<:calendar_up:639321092581883922>";
        public const string ICON_CALENDAR_STAR = "<:calendar_star:639320966555762718>";
        public const string ICON_CLOSE_CIRCLE = "<:close_circle:534605008310894593>";
        public const string ICON_ACCOUNT = "<:account:577564969080455178>";
        public const string ICON_ACCOUNT_MULTIPLE = "<:account_multiple:536166717718528001>";
        public const string ICON_MESSAGE_UP = "<:message_up:577143643136065567>";

        public const string BLANK = "<:blank:533671174547439626>";

        public static string GetEmoji(UserStatus status, ClientType type = ClientType.General) => type switch
        {
            ClientType.Desktop => status switch
            {
                UserStatus.Online => STATUS_DESKTOP_ONLINE,
                UserStatus.Idle => STATUS_DESKTOP_IDLE,
                UserStatus.DoNotDisturb => STATUS_DESKTOP_BUSY,
                _ => STATUS_OFFLINE,
            },
            ClientType.Mobile => status switch
            {
                UserStatus.Online => STATUS_MOBILE_ONLINE,
                UserStatus.Idle => STATUS_MOBILE_IDLE,
                UserStatus.DoNotDisturb => STATUS_MOBILE_BUSY,
                _ => STATUS_OFFLINE,
            },
            ClientType.Web => status switch
            {
                UserStatus.Online => STATUS_WEB_ONLINE,
                UserStatus.Idle => STATUS_WEB_IDLE,
                UserStatus.DoNotDisturb => STATUS_WEB_BUSY,
                _ => STATUS_OFFLINE,
            },
            _ => status switch
            {
                UserStatus.Online => STATUS_ONLINE,
                UserStatus.Idle => STATUS_IDLE,
                UserStatus.DoNotDisturb => STATUS_BUSY,
                _ => STATUS_OFFLINE,
            },
        };

        public enum ClientType
        {
            General,
            Desktop,
            Mobile,
            Web
        }
    }
}