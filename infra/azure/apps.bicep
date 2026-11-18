targetScope = 'resourceGroup'

param config object
param images object

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: config.environmentName
}
resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' existing = {
  name: config.registryName
}
resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: config.pullIdentityName
}

var blazorProxyKey = 'ReverseProxy__Clusters__backend__Destinations__backend__Address'

var definitions = [
  union(config.apps.aspnetApi, {
    image: images.aspnetApi
    port: 8080
    external: false
    env: []
  })
  union(config.apps.nodeApi, {
    image: images.nodeApi
    port: 5213
    external: false
    env: [{ name: 'HOST', value: '0.0.0.0' }, { name: 'PORT', value: '5213' }]
  })
  union(config.apps.blazorAspnet, {
    image: images.blazor
    port: 8080
    external: true
    env: []
    proxyKey: blazorProxyKey
    backendName: config.apps.aspnetApi.name
  })
  union(config.apps.blazorNode, {
    image: images.blazor
    port: 8080
    external: true
    env: []
    proxyKey: blazorProxyKey
    backendName: config.apps.nodeApi.name
  })
  union(config.apps.reactAspnet, {
    image: images.react
    port: 3000
    external: true
    env: [{ name: 'PORT', value: '3000' }]
    proxyKey: 'API_BASE_URL'
    backendName: config.apps.aspnetApi.name
  })
  union(config.apps.reactNode, {
    image: images.react
    port: 3000
    external: true
    env: [{ name: 'PORT', value: '3000' }]
    proxyKey: 'API_BASE_URL'
    backendName: config.apps.nodeApi.name
  })
]

module services './container-app.bicep' = [for (app, index) in definitions: {
  name: 'service-${index}'
  params: {
    location: config.location
    environmentId: environment.id
    registryServer: registry.properties.loginServer
    pullIdentityId: toLower(pullIdentity.id)
    app: union(app, {
      env: app.external ? concat(app.env, [{
        name: app.proxyKey
        value: 'https://${app.backendName}.internal.${environment.properties.defaultDomain}'
      }]) : app.env
    })
  }
}]

output publicUrls array = [
  'https://${config.apps.blazorAspnet.name}.${environment.properties.defaultDomain}'
  'https://${config.apps.blazorNode.name}.${environment.properties.defaultDomain}'
  'https://${config.apps.reactAspnet.name}.${environment.properties.defaultDomain}'
  'https://${config.apps.reactNode.name}.${environment.properties.defaultDomain}'
]
