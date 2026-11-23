#!/usr/bin/env python3
"""ejabberd external authentication: asks our API whether a Jabber login is valid.

ejabberd starts this program and talks to it over stdin/stdout. Every request is a 2-byte big-endian
length followed by "command:arg1:arg2:..."; every answer is the length 2 followed by a 2-byte integer,
1 for yes and 0 for no. A Jabber account is exactly a web account: same username and password.
"""
import json
import os
import struct
import sys
import urllib.request

API_URL = os.environ.get("XMPP_AUTH_URL", "http://api:8080/internal/xmpp").rstrip("/")
SECRET = os.environ.get("XMPP_AUTH_SECRET", "")


def call(path, body):
    request = urllib.request.Request(
        API_URL + path,
        data=json.dumps(body).encode("utf-8"),
        headers={"Content-Type": "application/json", "X-Xmpp-Auth-Secret": SECRET},
    )
    with urllib.request.urlopen(request, timeout=5) as response:
        return json.load(response).get("valid") is True


def handle(command, args):
    try:
        if command == "auth" and len(args) >= 3:
            # The password may itself contain ":", so everything after the server belongs to it.
            user, server, password = args[0], args[1], ":".join(args[2:])
            return call("/auth", {"user": user, "server": server, "password": password})
        if command == "isuser" and len(args) >= 2:
            return call("/isuser", {"user": args[0], "server": args[1]})
    except Exception as error:  # the API is unreachable or answered with an error: refuse
        print("extauth error:", error, file=sys.stderr, flush=True)
    # setpass, tryregister, removeuser ...: accounts are managed on the web site only
    return False


def read_exact(count):
    data = sys.stdin.buffer.read(count)
    return data if len(data) == count else None


def main():
    while True:
        header = read_exact(2)
        if header is None:
            return
        (length,) = struct.unpack(">H", header)
        payload = read_exact(length)
        if payload is None:
            return
        parts = payload.decode("utf-8", errors="replace").split(":")
        answer = 1 if handle(parts[0], parts[1:]) else 0
        sys.stdout.buffer.write(struct.pack(">HH", 2, answer))
        sys.stdout.buffer.flush()


if __name__ == "__main__":
    main()
