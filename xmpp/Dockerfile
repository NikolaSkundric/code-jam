# ejabberd (XMPP/Jabber server) with Python for the external-auth bridge to our API and our federation
# traffic counter module (mod_chat_stats) for the admin dashboard.
FROM ghcr.io/processone/ejabberd:latest

USER root
RUN apk add --no-cache python3
USER ejabberd

# Federation traffic counters module (compiled with the node of the image, so start one just for the build).
COPY --chown=ejabberd:ejabberd mod_chat_stats /opt/ejabberd/.ejabberd-modules/sources/chat/mod_chat_stats
RUN ejabberdctl start; ejabberdctl started; ejabberdctl module_install mod_chat_stats; ejabberdctl stop; ejabberdctl stopped; test -f /opt/ejabberd/.ejabberd-modules/mod_chat_stats/ebin/mod_chat_stats.beam

COPY --chown=ejabberd:ejabberd extauth.py /opt/ejabberd/extauth.py
COPY --chown=ejabberd:ejabberd ejabberd.yml /opt/ejabberd/conf/ejabberd.yml
