FROM node:24-bookworm-slim AS build
WORKDIR /app
COPY save-parser/package*.json ./
RUN npm ci
COPY save-parser/tsconfig.json ./
COPY save-parser/src ./src
RUN npm run build

FROM node:24-bookworm-slim AS final
ENV NODE_ENV=production
WORKDIR /app
COPY save-parser/package*.json ./
RUN npm ci --omit=dev --ignore-scripts \
    && npm cache clean --force
COPY --from=build /app/dist ./dist
USER node
EXPOSE 3000
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=10 \
  CMD node -e "fetch('http://127.0.0.1:3000/health').then(r=>{if(!r.ok)process.exit(1)}).catch(()=>process.exit(1))"
CMD ["node", "dist/server.js"]
