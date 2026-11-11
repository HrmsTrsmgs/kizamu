import { Project } from 'kizamu-domain-typescript'
import { useState } from 'react'

export function useProjectTabs() {
  const [projects, setProjects] = useState<Project[]>([])
  const [showProjectDialog, setShowProjectDialog] = useState(false)

  function openProjectDialog() {
    setShowProjectDialog(true)
  }

  function registerProject(name: string) {
    setProjects(projects => [...projects, new Project(name)])
  }

  return { state: { projects, showProjectDialog }, registerProject, openProjectDialog }
}
