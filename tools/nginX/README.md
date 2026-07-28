# Nginx Media Server

Nginx serves files from `vitrivr-engine/sandbox/media/` on port `9090`.

## 1. Create the media site

Create and open `/etc/nginx/sites-available/media`:

```bash
sudo nano /etc/nginx/sites-available/media
```

Paste this configuration:

```nginx
server {
    listen 9090;
    server_name _;

    location / {
        root /<pathToVitrivr>/vitrivr-engine/sandbox/media;
        try_files $uri =404;
    }
}
```

Set permissions:

```bash
sudo find /<pathToVitrivr>/vitrivr-engine/sandbox/media \
  -type d -exec chmod 755 {} \;
sudo find /<pathToVitrivr>/vitrivr-engine/sandbox/media \
  -type f -exec chmod 644 {} \;
```

Start the site:

```bash
sudo ln -s /etc/nginx/sites-available/media /etc/nginx/sites-enabled/media
sudo nginx -t
sudo systemctl restart nginx
sudo systemctl enable nginx
```

## 2. Configure the port

```bash
sudo ufw allow 9090/tcp
```

## Verify

Test an asset:

```bash
curl -I http://127.0.0.1:9090/images/<file>.jpg
```

From another machine, use:

```text
http://<serverIp>:9090/images/<file>.jpg
```

The backend is now complete. Continue with the frontend in [`../godot/README.md`](../godot/README.md).
