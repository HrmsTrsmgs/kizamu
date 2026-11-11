import { Project } from 'kizamu-domain-typescript'
import { useState } from 'react'

export function useProjectTabs() {
  const [projects, setProjects] = useState<Project[]>([])
  const [selectedProject, setSelectedProject] = useState<Project>()
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
    setProjectName('')
    setShowProjectDialog(false)
  }

  return { state: { projects, selectedProject, projectName, showProjectDialog }, setProjectName, registerProject, selectProject: setSelectedProject, openProjectDialog, cancelProjectDialog }
}
