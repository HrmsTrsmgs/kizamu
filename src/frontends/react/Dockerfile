FROM node:24.18.0-bookworm-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY src/domains/typescript/ src/domains/typescript/
COPY src/frontends/react/ src/frontends/react/
RUN npm ci --workspace=kizamu-frontend-react
RUN npm run build --workspace=kizamu-frontend-react

FROM node:24.18.0-bookworm-slim AS dependencies
WORKDIR /app
COPY package.json package-lock.json ./
COPY src/domains/typescript/package.json src/domains/typescript/package.json
COPY src/frontends/react/package.json src/frontends/react/package.json
RUN npm ci --omit=dev --workspace=kizamu-frontend-react

FROM node:24.18.0-bookworm-slim
WORKDIR /app
ENV NODE_ENV=production
COPY --from=dependencies /app/node_modules/ node_modules/
COPY --from=dependencies /app/src/ src/
COPY --from=build /app/src/frontends/react/dist/ src/frontends/react/dist/
COPY --from=build /app/src/frontends/react/server.mjs src/frontends/react/server.mjs
USER node
EXPOSE 3000
CMD ["node", "src/frontends/react/server.mjs"]
