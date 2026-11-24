import { useId } from 'react'
import type { Project } from 'kizamu-domain-typescript'

type ProjectTabsProps = {
  projects: readonly Project[]
  selectedProject: Project | undefined
  onAddProject: () => void
  onSelectProject: (project: Project | undefined) => void
}

export default function ProjectTabs({ projects, selectedProject, onAddProject, onSelectProject }: ProjectTabsProps) {
  const titleId = useId()

  return (
    <section className="project-toolbar" aria-labelledby={titleId}>
      <div className="project-toolbar-heading">
        <h2 id={titleId}>プロジェクト</h2>
        <button className="project-add-button" type="button" onClick={onAddProject}>追加</button>
      </div>
      <div className="project-tabs" role="tablist" aria-label="プロジェクト">
        <button
          className="project-default project-tab"
          role="tab"
          aria-selected={selectedProject === undefined}
          type="button"
          onClick={() => onSelectProject(undefined)}
        >未指定</button>
        {projects.map((project, index) => (
          <button
            className="project-tab"
            role="tab"
            aria-selected={selectedProject === project}
            type="button"
            key={index}
            onClick={() => onSelectProject(project)}
          >{project.name}</button>
        ))}
      </div>
    </section>
  )
}
