import { Project } from 'kizamu-domain-typescript'
import { useState } from 'react'

export function useProjectTabs() {
  const [projects, setProjects] = useState<Project[]>([])
  const [projectName, setProjectName] = useState('')
  const [showProjectDialog, setShowProjectDialog] = useState(false)

  function openProjectDialog() {
    setShowProjectDialog(true)
  }

  function cancelProjectDialog() {
    setShowProjectDialog(false)
  }

  function registerProject(name: string) {
    setProjects(projects => [...projects, new Project(name)])
    setShowProjectDialog(false)
  }

  return { state: { projects, projectName, showProjectDialog }, setProjectName, registerProject, openProjectDialog, cancelProjectDialog }
}
