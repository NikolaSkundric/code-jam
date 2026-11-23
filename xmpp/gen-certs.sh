#!/bin/sh
# Demo PKI for the two Jabber servers: one CA and one certificate per server domain, with the domain in
# subjectAltName, so that federation can use verified TLS. Both servers run from the same image, so each one
# trusts the other. The CA key is not secret here: this is a local demo; real deployments use a public CA.
set -e
mkdir -p /opt/chat-certs
cd /opt/chat-certs

openssl req -x509 -newkey rsa:2048 -nodes -keyout ca.key -out ca.pem -days 3650 -subj "/CN=Chat demo CA"

for host in a.chat.test b.chat.test; do
  openssl req -newkey rsa:2048 -nodes -keyout "$host.key" -out "$host.csr" -subj "/CN=$host"
  printf 'subjectAltName=DNS:%s,DNS:localhost\nextendedKeyUsage=serverAuth,clientAuth\n' "$host" > "$host.ext"
  openssl x509 -req -in "$host.csr" -CA ca.pem -CAkey ca.key -CAcreateserial -out "$host.crt" -days 3650 -extfile "$host.ext"
  cat "$host.crt" "$host.key" > "$host.pem"
done

rm -f ./*.csr ./*.ext ./*.srl
chown -R ejabberd:ejabberd /opt/chat-certs
chmod 640 /opt/chat-certs/*.key
