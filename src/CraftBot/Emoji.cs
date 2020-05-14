using DSharpPlus.Entities;

namespace CraftBot
{
    public static class Emoji
    {
        public const string Blank = "<:blank:533671174547439626>";
        public const string GenderFemale = "<:gender_female:538474670588952576>";
        public const string GenderMale = "<:gender_male:538474670844805130>";
        public const string IconAccount = "<:account:577564969080455178>";
        public const string IconAccountGroup = "<:accountgroup:647944149412216902>";
        public const string IconAccountMultiple = "<:account_multiple:536166717718528001>";
        public const string IconCakeVariant = "<:cakevariant:648291271609286685>";
        public const string IconCalendarPlus = "<:calendar_plus:534031917721452544>";
        public const string IconCalendarStar = "<:calendar_star:639320966555762718>";
        public const string IconCheckboxBlankOutline = "<:checkbox_blank_outline:653290881209729034>";
        public const string IconCheckboxMarked = "<:checkbox_marked:653290881125974017>";
        public const string IconCircleDouble = "<:circle_double:648993728777945098>";
        public const string IconClockOutline = "<:clockoutline:647944149391376394>";
        public const string IconCloseCircle = "<:close_circle:534605008310894593>";
        public const string IconCommand = "<:command:647937810799656960>";
        public const string IconDiscord = "<:discord:580739992419303435>";
        public const string IconDsharpplus = "<:dsharpplus:648275881156149284>";
        public const string IconMemory = "<:memory:647944149399502878>";
        public const string IconMessageUp = "<:message_up:577143643136065567>";
        public const string IconMonitor = "<:monitor:647944149424799754>";
        public const string IconPencil = "<:pencil:534032475358494720>";
        public const string IconTags = "<:tags:654054802380292115>";
        public const string IconTextureBox = "<:texture_box:648995869374545920>";
        public const string Busy = "<:dnd:637413123992846359>";
        public const string DesktopBusy = "<:desktop_busy:638943670661546014>";
        public const string DesktopIdle = "<:desktop_idle:638943670254567425>";
        public const string DesktopOnline = "<:desktop_online:638943670653026304>";
        public const string Idle = "<:idle:637413124193910811>";
        public const string MobileBusy = "<:mobile_busy:638943670225207297>";
        public const string MobileIdle = "<:mobile_idle:638943670476734495>";
        public const string MobileOnline = "<:mobile_online:638943670787244032>";
        public const string Offline = "<:offline:637413124550557709>";
        public const string Online = "<:online:637413124366139398>";
        public const string WebBusy = "<:web_busy:638943671017930753>";
        public const string WebIdle = "<:web_idle:638943670892232745>";
        public const string WebOnline = "<:web_online:638943671454269450>";
        public const string FlagAlman = "<:flag_alman:654769827952459787>";
        public const string IconKey = "<:key:691737255798243339>";
        public const string IconLink = "<:link:691737677124599839>";
        public const string IconLinkOff = "<:link_off:691737677082656769>";
        public const string IconCheck = "<:check:580739992633212968>";
        public const string IconClose = "<:close:691740675489923122>";
        public const string IconCog = "<:cog:693878051062415360>";
        public const string IconHeadphonesOff = "<:headphones_off:693882945898807316>";
        public const string IconHeadphonesOffRed = "<:headphones_off_red:693882945911652382>";
        public const string IconMicrophoneOff = "<:microphone_off:693882946142339082>";
        public const string IconMicrophoneOffRed = "<:microphone_off_red:693882946154660002>";
        public const string IconStar = "<:star:534032132561960994>";
        
        public static string GetEmoji(this UserStatus status) => status switch
        {
            UserStatus.Online => Online,
            UserStatus.Idle => Idle,
            UserStatus.DoNotDisturb => Busy,
            _ => Offline
        };
    }
}