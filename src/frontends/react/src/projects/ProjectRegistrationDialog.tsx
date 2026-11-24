import { useId } from 'react'

type ProjectRegistrationDialogProps = {
  projectName: string
  onProjectNameChange: (name: string) => void
  onRegister: () => void
  onCancel: () => void
}

export default function ProjectRegistrationDialog({
  projectName,
  onProjectNameChange,
  onRegister,
  onCancel,
}: ProjectRegistrationDialogProps) {
  const dialogId = useId()
  const titleId = `${dialogId}-title`
  const nameInputId = `${dialogId}-name`

  return (
    <div className="project-dialog-overlay">
      <div className="project-dialog" role="dialog" aria-labelledby={titleId}>
        <h2 id={titleId}>プロジェクトを追加</h2>
        <label htmlFor={nameInputId}>プロジェクト名</label>
        <input id={nameInputId} value={projectName} onChange={event => onProjectNameChange(event.target.value)} />
        <div className="project-dialog-actions">
          <button className="project-cancel-button" type="button" onClick={onCancel}>キャンセル</button>
          <button className="project-register-button" type="button" onClick={onRegister}>登録</button>
        </div>
      </div>
    </div>
  )
}
