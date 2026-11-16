FROM node:24.18.0-bookworm-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY src/domains/typescript/ src/domains/typescript/
COPY src/backends/node/ src/backends/node/
RUN npm ci --workspace=kizamu-backend-node
RUN npm run build --workspace=kizamu-backend-node

FROM node:24.18.0-bookworm-slim AS dependencies
WORKDIR /app
COPY package.json package-lock.json ./
COPY src/domains/typescript/package.json src/domains/typescript/package.json
COPY src/backends/node/package.json src/backends/node/package.json
RUN npm ci --omit=dev --workspace=kizamu-backend-node

FROM node:24.18.0-bookworm-slim
WORKDIR /app
ENV NODE_ENV=production
COPY --from=dependencies /app/node_modules/ node_modules/
COPY --from=dependencies /app/src/ src/
COPY --from=build /app/src/domains/typescript/dist/ src/domains/typescript/dist/
COPY --from=build /app/src/backends/node/dist/ src/backends/node/dist/
USER node
EXPOSE 5213
CMD ["node", "src/backends/node/dist/server.js"]
