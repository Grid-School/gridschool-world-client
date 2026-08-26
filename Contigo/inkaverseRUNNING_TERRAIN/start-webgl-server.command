#!/bin/bash

# --- CONFIG ---
BUILD_DIR="Build"  # Adjust if your Unity WebGL build folder is different
PORT=5000
CERT="localhost.pem"
KEY="localhost-key.pem"

# --- Check for certs ---
if [[ ! -f "$CERT" || ! -f "$KEY" ]]; then
  echo "Generating self-signed certs for HTTPS (localhost)..."
  openssl req -newkey rsa:2048 -nodes -keyout $KEY -x509 -days 365 -out $CERT -subj "/C=US/ST=CA/L=UnityDev/O=LocalDev/OU=Test/CN=localhost"
fi

# --- Run server ---
echo "Starting HTTPS server on https://localhost:$PORT"
http-server $BUILD_DIR -p $PORT --ssl --cert $CERT --key $KEY --brotli --gzip
