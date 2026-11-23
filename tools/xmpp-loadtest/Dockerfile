FROM node:22-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev
COPY *.mjs ./
# The Jabber servers of this demo use self-signed certificates.
ENV NODE_TLS_REJECT_UNAUTHORIZED=0
CMD ["node", "loadtest.mjs"]
