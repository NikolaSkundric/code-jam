FROM node:22-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev
COPY loadtest.mjs ./
CMD ["node", "loadtest.mjs"]
