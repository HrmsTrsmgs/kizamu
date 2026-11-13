window.kizamuGoogleSignIn = (container, clientId, receiver) => {
  google.accounts.id.initialize({
    client_id: clientId,
    callback: response => receiver.invokeMethodAsync('ReceiveCredential', response.credential),
  })
  google.accounts.id.renderButton(container, { type: 'standard' })
}
