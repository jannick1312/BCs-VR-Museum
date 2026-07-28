# vitrivr Backend

This guide installs vitrivr Engine and the Python Descriptor Server outside this repository. It follows the official [vitrivr Engine Getting Started guide](https://github.com/vitrivr/vitrivr-engine/wiki/Getting-Started). It also adds the tested configuration, descriptor service, media layout, and helper scripts. The files in `tools/vitrivr/` are copied into the cloned repositories.

## File destinations

| Source in `tools/vitrivr/` | Destination |
|---|---|
| `configs/config-schema.json` | `vitrivr-engine/config-schema.json` |
| `configs/*-ingest.json` | `vitrivr-engine/example-configs/` |
| `descriptor-server/requirements.txt` | `vitrivr-python-descriptor-server/requirements.txt` |
| `descriptor-server/startup.sh` | `vitrivr-python-descriptor-server/startup.sh` |
| `scripts/*.sh` | Beside both cloned repositories |

## 1. Clone and build

```bash
mkdir -p /<pathToVitrivr>
cd /<pathToVitrivr>

git clone https://github.com/vitrivr/vitrivr-engine.git
git clone https://github.com/vitrivr/vitrivr-python-descriptor-server.git

cd vitrivr-engine
./gradlew clean installDist
```

## 2. Prepare the Descriptor Server

```bash
cp /<pathTo>/tools/vitrivr/descriptor-server/requirements.txt \
  /<pathToVitrivr>/vitrivr-python-descriptor-server/requirements.txt

cp /<pathTo>/tools/vitrivr/descriptor-server/startup.sh \
  /<pathToVitrivr>/vitrivr-python-descriptor-server/startup.sh

cd /<pathToVitrivr>/vitrivr-python-descriptor-server
python3 -m venv features
source features/bin/activate
pip install -r requirements.txt
deactivate
```

## 3. Configure PostgreSQL

```bash
sudo systemctl enable --now postgresql
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'password';"
```

The password is only for the controlled internal server and must match the password set in `config-schema.json` and `reset-vitrivr.sh`.

## 4. Copy the configuration and scripts

```bash
cp /<pathTo>/tools/vitrivr/configs/config-schema.json \
  /<pathToVitrivr>/vitrivr-engine/config-schema.json

cp /<pathTo>/tools/vitrivr/configs/3d-ingest.json \
  /<pathToVitrivr>/vitrivr-engine/example-configs/3d-ingest.json
cp /<pathTo>/tools/vitrivr/configs/image-ingest.json \
  /<pathToVitrivr>/vitrivr-engine/example-configs/image-ingest.json
cp /<pathTo>/tools/vitrivr/configs/video-ingest.json \
  /<pathToVitrivr>/vitrivr-engine/example-configs/video-ingest.json

cp /<pathTo>/tools/vitrivr/scripts/start-vitrivr-tmux.sh \
  /<pathToVitrivr>/start-vitrivr-tmux.sh
cp /<pathTo>/tools/vitrivr/scripts/reset-vitrivr.sh \
  /<pathToVitrivr>/reset-vitrivr.sh

chmod +x /<pathToVitrivr>/start-vitrivr-tmux.sh \
  /<pathToVitrivr>/reset-vitrivr.sh
```

On a new installation, reset the public database schema and activate pgvector:

```bash
cd /<pathToVitrivr>
./reset-vitrivr.sh
```

This command also stops an existing vitrivr tmux session and Xvfb process. It deletes all vitrivr data and all data in the PostgreSQL `public` schema. It resets everything so use it only for a new installation or a reset.

## 5. Create the sandbox

```bash
mkdir -p \
  /<pathToVitrivr>/vitrivr-engine/sandbox/media/3d \
  /<pathToVitrivr>/vitrivr-engine/sandbox/media/images \
  /<pathToVitrivr>/vitrivr-engine/sandbox/media/videos \
  /<pathToVitrivr>/vitrivr-engine/sandbox/thumbnails/3d \
  /<pathToVitrivr>/vitrivr-engine/sandbox/thumbnails/images \
  /<pathToVitrivr>/vitrivr-engine/sandbox/thumbnails/videos
```

Place originals only in the matching `sandbox/media/` directories. The thumbnail directories must exist before ingestion or the ingestion steps will fail.

## 6. Prepare the media

Copy and run the pipeline according to [`../pipeline/README.md`](../pipeline/README.md).

## 7. Start and ingest

```bash
cd /<pathToVitrivr>
./start-vitrivr-tmux.sh
```

In the vitrivr Engine tmux window:

```text
v> sandbox init
v> sandbox extract -n 3d
v> sandbox extract -n image
v> sandbox extract -n video
```

## Verify

```bash
tmux has-session -t vitrivr
curl -f http://localhost:8888/health
curl -f http://localhost:7070/api/schema/list
```

For later starts, run only `./start-vitrivr-tmux.sh` or attach to the running tmux session. To start again, run `./reset-vitrivr.sh` and repeat `sandbox init` and all extraction commands.

Next, configure media access with [`../nginX/README.md`](../nginX/README.md).
