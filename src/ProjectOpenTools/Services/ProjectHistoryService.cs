using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectOpenTools.Models;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责维护最近项目列表的排序与去重。
/// </summary>
public sealed class ProjectHistoryService
{
    /// <summary>
    /// 将项目加入最近列表，若已存在则仅更新时间并置顶。
    /// </summary>
    public void UpsertRecentProject(List<ProjectEntry> recentProjects, string projectPath, DateTime openedAt)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return;
        }

        ProjectEntry? existingProject = recentProjects.FirstOrDefault(item => string.Equals(item.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        if (existingProject == null)
        {
            recentProjects.Add(new ProjectEntry
            {
                Path = projectPath,
                DisplayName = Path.GetFileName(projectPath),
                LastOpenedAt = openedAt
            });
        }
        else
        {
            existingProject.DisplayName = Path.GetFileName(projectPath);
            existingProject.LastOpenedAt = openedAt;
        }

        List<ProjectEntry> orderedProjects = recentProjects
            .OrderByDescending(item => item.LastOpenedAt)
            .ToList();

        recentProjects.Clear();
        foreach (ProjectEntry orderedProject in orderedProjects)
        {
            recentProjects.Add(orderedProject);
        }
    }
}
