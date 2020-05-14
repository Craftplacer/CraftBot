using System;

namespace CraftBot.Model
{
    [Flags]
    public enum OneTimeNotices
    {
        /// <summary>
        /// Default value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Notice for informing the user that they're trying to quote without having it enabled.
        /// </summary>
        Quoting,

        /// <summary>
        /// Notice for asking the user permission to use context data, to know what input they gave to make the executing command crash.
        /// </summary>
        ExceptionAnalytics,
        UserSurvey
    }
}