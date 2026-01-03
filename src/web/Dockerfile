# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source
COPY . .

# Build
RUN npm run build

# Runtime stage
FROM node:22-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Copy built application
COPY --from=build /app/build ./build
COPY --from=build /app/package*.json ./

# Install production dependencies only
RUN npm ci --omit=dev

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000
ENV ORIGIN=https://mujdomecek.x213.cz

CMD ["node", "build"]
