# ejabberd (XMPP/Jabber server) with Python for the external-auth bridge to our API and our federation
# traffic counter module (mod_chat_stats) for the admin dashboard.
FROM ghcr.io/processone/ejabberd:latest

USER root
RUN apk add --no-cache python3 openssl

# Demo PKI for verified TLS between the two servers (see gen-certs.sh). Lives outside /opt/ejabberd, which the
# base image declares as a volume.
COPY gen-certs.sh /tmp/gen-certs.sh
RUN sh /tmp/gen-certs.sh && rm /tmp/gen-certs.sh
USER ejabberd

# Our ejabberd modules: federation traffic counters and the bridge to the web API (compiled with the node of
# the image, so start one just for the build).
COPY --chown=ejabberd:ejabberd mod_chat_stats /opt/ejabberd/.ejabberd-modules/sources/chat/mod_chat_stats
COPY --chown=ejabberd:ejabberd mod_chat_bridge /opt/ejabberd/.ejabberd-modules/sources/chat/mod_chat_bridge
COPY install-module.sh /tmp/install-module.sh
RUN sh /tmp/install-module.sh

COPY --chown=ejabberd:ejabberd extauth.py /opt/ejabberd/extauth.py
COPY --chown=ejabberd:ejabberd ejabberd.yml /opt/ejabberd/conf/ejabberd.yml
