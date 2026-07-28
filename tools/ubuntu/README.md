# Ubuntu Server Setup

The server uses Ubuntu 24.04.4 LTS on x86-64.

## 1. Install the Ubuntu packages

```bash
sudo apt update
sudo apt upgrade -y

sudo apt install -y \
  git curl ca-certificates apt-transport-https gnupg unzip zip \
  python3 python3-pip python3.12-venv python3-flask \
  tmux nginx ufw openjdk-21-jdk \
  xvfb x11-utils mesa-utils \
  libgl1 libglib2.0-0t64 libgl1-mesa-dri libglx-mesa0 \
  libegl1 libgles2 libxrender1 libxi6 libxrandr2 \
  libxcursor1 libxinerama1 libxxf86vm1 \
  imagemagick ffmpeg
```

## 2. Install PostgreSQL 17 and pgvector

```bash
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/postgresql.gpg

sudo apt update
sudo apt install -y postgresql-17 postgresql-17-pgvector
sudo systemctl enable --now postgresql
```

The database password and `vector` extension are configured in [`../vitrivr/README.md`](../vitrivr/README.md).

## 3. Install Node.js 22 and glTF-Transform

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt install -y nodejs
sudo npm install -g @gltf-transform/cli
```

## 4. Install KTX-Software 4.4.2

```bash
curl -fL \
  -o KTX-Software.deb \
  "https://github.com/KhronosGroup/KTX-Software/releases/download/v4.4.2/KTX-Software-4.4.2-Linux-x86_64.deb"
sudo apt install ./KTX-Software.deb
```

## 5. Install Godot 4.6.2

```bash
curl -fL \
  'https://downloads.godotengine.org/?flavor=stable&platform=linux.64&slug=linux.x86_64.zip&version=4.6.2' \
  -o /tmp/godot-4.6.2.zip

sudo mkdir -p /opt/godot/4.6.2
sudo unzip -j /tmp/godot-4.6.2.zip -d /opt/godot/4.6.2
sudo chmod +x /opt/godot/4.6.2/Godot_v4.6.2-stable_linux.x86_64
sudo ln -sfn \
  /opt/godot/4.6.2/Godot_v4.6.2-stable_linux.x86_64 \
  /usr/local/bin/godot
```

Install the matching export templates as the account that will run the media pipeline:

```bash
curl -fL \
  'https://downloads.godotengine.org/?flavor=stable&platform=templates&slug=export_templates.tpz&version=4.6.2' \
  -o /tmp/godot-4.6.2-templates.tpz

template_tmp="$(mktemp -d)"
unzip -q /tmp/godot-4.6.2-templates.tpz -d "$template_tmp"
mkdir -p "$HOME/.local/share/godot/export_templates/4.6.2.stable"
cp -a "$template_tmp/templates/." \
  "$HOME/.local/share/godot/export_templates/4.6.2.stable/"
rm -rf "$template_tmp"
```

## Verify

```bash
git --version
curl --version
python3 --version
pip3 --version
psql --version
java --version
node --version
npm --version
gltf-transform --version
godot --headless --version
ktx --version
nginx -v
tmux -V
ffmpeg -version
magick -version || convert -version
sudo systemctl is-active postgresql
ffmpeg -hide_banner -encoders | grep -E 'libtheora|libvorbis'
```

Node.js must report version 22, Godot version 4.6.2, KTX version 4.4.2, and FFmpeg must list both encoders.

Continue with [`../vitrivr/README.md`](../vitrivr/README.md).
