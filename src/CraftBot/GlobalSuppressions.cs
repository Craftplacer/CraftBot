// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is an expected exception whenever the bot tries to shutdown. I don't know why C# does this in the first place and then complains about not rethrowing it, we don't want retarded crashes.", Scope = "member", Target = "~M:CraftBot.Program.MainAsync(System.String[])~System.Threading.Tasks.Task")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "The name of the property should indicate that it shouldn't be used by the developer. As it's being used by LiteDB", Scope = "member", Target = "~P:CraftBot.Database.UserData._language")]