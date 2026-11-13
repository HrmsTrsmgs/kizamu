import { createServer, request as httpRequest } from 'node:http'
import { request as httpsRequest } from 'node:https'

const apiBaseUrl = new URL(process.env.API_BASE_URL)
const server = createServer((request, response) => {
  if (!request.url?.startsWith('/api/')) {
    response.writeHead(404).end()
    return
  }

  const target = new URL(request.url, apiBaseUrl)
  const sendRequest = target.protocol === 'https:' ? httpsRequest : httpRequest
  const upstream = sendRequest(target, { method: request.method, headers: { ...request.headers, host: target.host } }, upstreamResponse => {
    response.writeHead(upstreamResponse.statusCode, upstreamResponse.headers)
    upstreamResponse.pipe(response)
  })
  upstream.on('error', () => response.writeHead(502).end())
  request.pipe(upstream)
})

server.listen(Number(process.env.PORT ?? 3000), () => {
  console.log(`http://localhost:${server.address().port}`)
})
