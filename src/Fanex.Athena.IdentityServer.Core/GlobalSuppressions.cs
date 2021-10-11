// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters", Justification = "Allow generic with 4 params", Scope = "member", Target = "~M:Fanex.Athena.IdentityServer.Core.Extensions.TaskExtension.WhenAll``4(System.Threading.Tasks.Task{``0},System.Threading.Tasks.Task{``1},System.Threading.Tasks.Task{``2},System.Threading.Tasks.Task{``3})~System.Threading.Tasks.Task{System.ValueTuple{``0,``1,``2,``3}}")]
[assembly: SuppressMessage("Major Code Smell", "S3995:URI return values should not be strings", Justification = "<Pending>", Scope = "member", Target = "~M:Fanex.Athena.IdentityServer.Core.Validators.LrfPasswordDelegationValidator.GetDefaultReportUrl(System.Collections.Generic.IEnumerable{Fanex.Athena.IdentityServer.Core.Models.LrfFunction})~System.String")]
