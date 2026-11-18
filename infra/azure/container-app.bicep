param location string
param environmentId string
param registryServer string
param pullIdentityId string
param app object

resource service 'Microsoft.App/containerApps@2025-01-01' = {
  name: app.name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${pullIdentityId}': {} }
  }
  properties: {
    environmentId: environmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      maxInactiveRevisions: 100
      ingress: {
        external: app.external
        targetPort: app.port
        exposedPort: 0
        transport: 'auto'
        allowInsecure: false
        traffic: [{ latestRevision: true, weight: 100 }]
      }
      registries: [{ server: registryServer, identity: pullIdentityId }]
    }
    template: {
      containers: [
        {
          name: app.name
          image: app.image
          env: app.env
          resources: { cpu: json(app.cpu), memory: app.memory }
        }
      ]
      scale: { minReplicas: app.minReplicas, maxReplicas: 1, cooldownPeriod: 300, pollingInterval: 30 }
    }
  }
}
