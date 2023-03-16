using System.Text;
using Atlassian.Jira;

namespace BranchActualizer.Branches.MergeResolver;

public class JiraUsersIssuesBranchMergeSolver : CompositeBranchMergeResolver, IBranchAuthorResolver
{
    private readonly Jira _jira;

    private readonly IEnumerable<string>? _users;
    
    private readonly string? _project;
    
    private readonly string? _actualizeSince;

    private const string Or = " OR ";

    private string? _jql;

    private List<Issue>? _issues;

    public JiraUsersIssuesBranchMergeSolver(Jira jira, IEnumerable<string>? users, string? project = null, string? actualizeSince = null)
    {
        _jira = jira;
        _users = users;
        _project = project;
        _actualizeSince = actualizeSince;
    }

    protected override async Task<bool> ShouldMerge(BranchInfo request, CancellationToken cancellationToken = default)
    {
        return (await GetIssues()).Any(i => request.Name.Contains(i.Key.Value));
    }

    protected override async Task<string> ToFilter(FilterInfo info, CancellationToken cancellationToken = default)
    {
        return string.Empty;
        var sb = new StringBuilder();

        foreach (var issue in await GetIssues())
        {
            sb.Append($"name ~ \"{issue.Key.Value}\"");
            sb.Append(Or);
        }

        sb.Remove(sb.Length - Or.Length, Or.Length);
        return sb.ToString();
    }

    private async Task<List<Issue>> GetIssues()
    {
        if (_issues != null) return _issues;
        
        _issues = new List<Issue>();
        int total = 1;
        while (_issues.Count < total)
        {
            var issues = await _jira.Issues.GetIssuesFromJqlAsync(GetJql(), int.MaxValue, _issues.Count);
            total = issues.TotalItems;
            _issues.AddRange(issues);
        }

        return _issues;
    }

    private string GetJql()
    {
        if (_jql is not null) return _jql;
        
        var sb = new StringBuilder();

        if (_users == null)
            return sb.ToString();

        sb.Append("(");
        foreach (var user in _users)
        {
            sb.Append($"assignee = '{user}'");
            sb.Append(Or);
        }

        sb.Remove(sb.Length - Or.Length, Or.Length);
        sb.Append(")");
        if (!string.IsNullOrEmpty(_project))
        {
            sb.Append($" AND project = '{_project}'");
            sb.Append($" AND key >= '{_project}-{_actualizeSince ?? "0"}'");
        }

        _jql = sb.ToString();

        return _jql;
    }

    public async Task<string?> GetAuthorAsync(string branch, CancellationToken cancellationToken = default)
    {
        return (await GetIssues()).Where(x => branch.Contains(x.Key.Value)).Select(x => x.AssigneeUser?.AccountId).FirstOrDefault();
    }
}