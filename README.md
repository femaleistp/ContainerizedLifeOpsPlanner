# Containerized LifeOpsPlanner – IT435 Final

## Requirements
- Docker Desktop (Windows tested)

## Setup

1. Clone the repository:

   git clone <repo-url>
   cd ContainerizedLifeOpsPlanner

2. Create environment file:

   Copy `.env.example` to `.env`

3. Run containers:

   docker compose up -d --build

4. Verify containers:

   docker compose ps

   The `db` container should show `healthy`.

5. View console output:

   docker compose logs app --tail 50

6. Stop containers:

   docker compose down
