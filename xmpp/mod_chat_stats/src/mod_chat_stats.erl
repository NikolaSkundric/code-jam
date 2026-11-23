%%% Federation traffic counters for the admin dashboard.
%%%
%%% Every stanza a local user sends to, or receives from, a user on another Jabber server is counted per
%%% remote domain, direction and stanza kind. The counters live in a RAM mnesia table and are published
%%% as plain text at /stats on the internal HTTP listener (one "direction peer kind count" line each).
-module(mod_chat_stats).
-behaviour(gen_mod).

-export([start/2, stop/1, reload/3, depends/2, mod_options/1, mod_opt_type/1, mod_doc/0]).
-export([user_send_packet/1, user_receive_packet/1, process/2]).

-include_lib("xmpp/include/xmpp.hrl").
-include("ejabberd_http.hrl").

-define(TABLE, mod_chat_stats_counters).

start(Host, _Opts) ->
    case mnesia:create_table(?TABLE, [{ram_copies, [node()]}, {attributes, [key, count]}]) of
        {atomic, ok} -> ok;
        {aborted, {already_exists, _}} -> ok
    end,
    ejabberd_hooks:add(user_send_packet, Host, ?MODULE, user_send_packet, 90),
    ejabberd_hooks:add(user_receive_packet, Host, ?MODULE, user_receive_packet, 90),
    ok.

stop(Host) ->
    ejabberd_hooks:delete(user_send_packet, Host, ?MODULE, user_send_packet, 90),
    ejabberd_hooks:delete(user_receive_packet, Host, ?MODULE, user_receive_packet, 90),
    ok.

reload(_Host, _NewOpts, _OldOpts) -> ok.

depends(_Host, _Opts) -> [].

mod_opt_type(_) -> econf:any().

mod_options(_Host) -> [].

mod_doc() ->
    #{desc => <<"Counts the stanzas exchanged with other Jabber servers per remote domain.">>}.

%% Stanza sent by a local user: outgoing federation traffic if it is addressed to another server.
user_send_packet({Packet, State}) ->
    count(out, xmpp:get_to(Packet), Packet, State),
    {Packet, State}.

%% Stanza delivered to a local user: incoming federation traffic if it comes from another server.
user_receive_packet({Packet, State}) ->
    count(in, xmpp:get_from(Packet), Packet, State),
    {Packet, State}.

count(Direction, #jid{lserver = Peer}, Packet, #{lserver := Local}) when Peer /= Local, Peer /= <<>> ->
    Key = {Direction, Peer, kind(Packet)},
    catch mnesia:dirty_update_counter(?TABLE, Key, 1),
    ok;
count(_Direction, _Jid, _Packet, _State) ->
    ok.

kind(Packet) when element(1, Packet) == message -> message;
kind(Packet) when element(1, Packet) == presence -> presence;
kind(Packet) when element(1, Packet) == iq -> iq;
kind(_) -> other.

%% HTTP handler: GET /stats
process(_Path, #request{}) ->
    Rows = lists:sort(mnesia:dirty_match_object({?TABLE, '_', '_'})),
    Lines = [io_lib:format("~s ~s ~s ~B~n", [Direction, Peer, Kind, Count])
             || {_, {Direction, Peer, Kind}, Count} <- Rows],
    {200, [{<<"Content-Type">>, <<"text/plain; charset=utf-8">>}], iolist_to_binary(Lines)}.
