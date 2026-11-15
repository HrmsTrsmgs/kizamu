import { createServer, request as httpRequest } from 'node:http'
import { request as httpsRequest } from 'node:https'
import { fileURLToPath } from 'node:url'
import serveHandler from 'serve-handler'

const apiBaseUrl = new URL(process.env.API_BASE_URL)
const distPath = fileURLToPath(new URL('./dist/', import.meta.url))
const server = createServer(async (request, response) => {
  if (!request.url?.startsWith('/api/')) {
    await serveHandler(request, response, { public: distPath, directoryListing: false, cleanUrls: false, rewrites: [{ source: '/', destination: '/index.html' }] })
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
