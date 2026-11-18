targetScope = 'resourceGroup'

param config object

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: config.logsName
  location: config.location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: config.registryName
  location: config.location
  sku: { name: config.registrySku }
  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
    dataEndpointEnabled: false
    networkRuleBypassAllowedForTasks: false
    encryption: { status: 'disabled' }
    policies: { azureADAuthenticationAsArmPolicy: { status: 'enabled' } }
    publicNetworkAccess: 'Enabled'
    roleAssignmentMode: 'LegacyRegistryPermissions'
  }
}

resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: config.pullIdentityName
  location: config.location
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: config.environmentName
  location: config.location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
    zoneRedundant: false
    peerAuthentication: { mtls: { enabled: false } }
    peerTrafficConfiguration: { encryption: { enabled: false } }
  }
}

output registryId string = registry.id
output registryServer string = registry.properties.loginServer
output pullIdentityId string = pullIdentity.id
output pullPrincipalId string = pullIdentity.properties.principalId
output environmentId string = environment.id
