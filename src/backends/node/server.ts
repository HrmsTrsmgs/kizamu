import { createApp } from './app.js'

const app = createApp()
const port = Number(process.env.PORT ?? 5213)
const address = await app.listen({ host: process.env.HOST ?? 'localhost', port })

console.log(address)
