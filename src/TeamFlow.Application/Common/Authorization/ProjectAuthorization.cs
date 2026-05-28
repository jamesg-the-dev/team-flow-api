using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Common.Authorization;

/// <summary>Project-scope authorization helpers used by command handlers.</summary>
internal static class ProjectAuthorization
{
    public static bool IsMember(Project project, Guid userId) =>
        project.Members.Any(m => m.UserId == userId);

    /// <summary>
    /// "Manager+" given the project-member role hierarchy
    /// (<see cref="ProjectMemberRole.Lead"/> &gt; Contributor &gt; Viewer).
    /// </summary>
    public static bool IsLead(Project project, Guid userId) =>
        project.Members.Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead);
}
