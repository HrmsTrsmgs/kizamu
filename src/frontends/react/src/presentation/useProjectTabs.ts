import { Project } from 'kizamu-domain-typescript'
import { useState } from 'react'

export function useProjectTabs() {
  const [projects, setProjects] = useState<Project[]>([])

  function registerProject(name: string) {
    setProjects(projects => [...projects, new Project(name)])
  }

  return { state: { projects }, registerProject }
}
