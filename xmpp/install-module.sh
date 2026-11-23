#!/bin/sh
# Compiles and installs our ejabberd modules (mod_chat_stats, mod_chat_bridge) with the node of the image.
# Starting a node during the build is occasionally flaky, so the whole sequence is retried.
MODULES="/opt/ejabberd/.ejabberd-modules/mod_chat_stats/ebin/mod_chat_stats.beam /opt/ejabberd/.ejabberd-modules/mod_chat_bridge/ebin/mod_chat_bridge.beam"
for attempt in 1 2 3 4; do
  ejabberdctl start
  ejabberdctl started
  ejabberdctl module_install mod_chat_stats
  ejabberdctl module_install mod_chat_bridge
  ejabberdctl stop
  ejabberdctl stopped
  missing=0
  for beam in $MODULES; do
    [ -f "$beam" ] || missing=1
  done
  if [ "$missing" = 0 ]; then
    echo "modules installed (attempt $attempt)"
    exit 0
  fi
  echo "attempt $attempt failed, retrying"
  sleep 5
done
echo "the ejabberd modules could not be installed" >&2
exit 1
