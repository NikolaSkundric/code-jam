%%% Bridge between Jabber clients and the website.
%%%
%%% Chat messages between two users of this server, and the presence of local users, are reported to the web
%%% API (POST <XMPP_AUTH_URL>/message and /presence, authenticated with XMPP_AUTH_SECRET). The API stores the
%%% messages in the personal chat of the two users and shows Jabber sessions as online status on the website.
%%% Nothing happens when XMPP_AUTH_URL is not set (the second, independent demo server). Messages that the web
%%% API itself sends to Jabber users are routed by the send_message command, which does not run these hooks,
%%% so there is no feedback loop.
-module(mod_chat_bridge).
-behaviour(gen_mod).

-export([start/2, stop/1, reload/3, depends/2, mod_options/1, mod_opt_type/1, mod_doc/0]).
-export([user_send_packet/1, sm_register_connection/3, sm_remove_connection/3, sm_remove_connection/4]).

-include_lib("xmpp/include/xmpp.hrl").
-include("logger.hrl").

start(Host, _Opts) ->
    ejabberd_hooks:add(user_send_packet, Host, ?MODULE, user_send_packet, 80),
    ejabberd_hooks:add(sm_register_connection_hook, Host, ?MODULE, sm_register_connection, 80),
    ejabberd_hooks:add(sm_remove_connection_hook, Host, ?MODULE, sm_remove_connection, 80),
    ok.

stop(Host) ->
    ejabberd_hooks:delete(user_send_packet, Host, ?MODULE, user_send_packet, 80),
    ejabberd_hooks:delete(sm_register_connection_hook, Host, ?MODULE, sm_register_connection, 80),
    ejabberd_hooks:delete(sm_remove_connection_hook, Host, ?MODULE, sm_remove_connection, 80),
    ok.

reload(_Host, _NewOpts, _OldOpts) -> ok.

depends(_Host, _Opts) -> [].

mod_opt_type(_) -> econf:any().

mod_options(_Host) -> [].

mod_doc() ->
    #{desc => <<"Reports chat messages and presence of local users to the chat web API.">>}.

%% A message from a local user to another user of the same server.
user_send_packet({#message{type = Type} = Packet, #{lserver := Host}} = Acc)
  when Type == chat; Type == normal ->
    Body = xmpp:get_text(Packet#message.body),
    From = xmpp:get_from(Packet),
    To = xmpp:get_to(Packet),
    case Body /= <<>> andalso To#jid.lserver == Host andalso To#jid.luser /= <<>> of
        true ->
            post("/message", [{from, From#jid.luser}, {to, To#jid.luser},
                              {body, Body}, {id, xmpp:get_id(Packet)}]);
        false ->
            ok
    end,
    Acc;
%% A presence update that the client broadcasts to its contacts: available (with the show value) or unavailable.
user_send_packet({#presence{type = Type, to = To} = Packet,
                  #{lserver := Host, user := User, resource := Resource}} = Acc)
  when Type == available; Type == unavailable ->
    case To#jid.luser == <<>> orelse (To#jid.luser == User andalso To#jid.lserver == Host) of
        true ->
            Show = case Packet#presence.show of undefined -> <<"">>; S -> atom_to_binary(S) end,
            post("/presence", [{user, User}, {resource, Resource}, {event, <<"presence">>},
                               {type, atom_to_binary(Type)}, {show, Show}]);
        false ->
            ok
    end,
    Acc;
user_send_packet(Acc) ->
    Acc.

sm_register_connection(_SID, #jid{luser = User, lresource = Resource}, _Info) ->
    post("/presence", [{user, User}, {resource, Resource}, {event, <<"open">>}]),
    ok.

%% The number of arguments of this hook differs between ejabberd versions: handle both.
sm_remove_connection(_SID, #jid{luser = User, lresource = Resource}, _Info) ->
    post("/presence", [{user, User}, {resource, Resource}, {event, <<"close">>}]),
    ok.

sm_remove_connection(SID, JID, Info, _Reason) ->
    sm_remove_connection(SID, JID, Info).

%% ---------------------------------------------------------------- HTTP to the web API

post(Path, Fields) ->
    case {os:getenv("XMPP_AUTH_URL"), os:getenv("XMPP_AUTH_SECRET")} of
        {false, _} ->
            ok;
        {Url, Secret} ->
            Json = json(Fields),
            spawn(fun() -> catch send(Url ++ Path, Secret, Json) end),
            ok
    end.

send(Url, Secret, Json) ->
    application:ensure_all_started(inets),
    case httpc:request(post, {Url, [{"X-Xmpp-Auth-Secret", Secret}], "application/json", Json},
                       [{timeout, 5000}, {connect_timeout, 2000}], []) of
        {ok, _} -> ok;
        {error, Reason} -> ?WARNING_MSG("Could not report to the chat API: ~p", [Reason])
    end.

json(Fields) ->
    Pairs = [[quote(K), ":", quote(V)] || {K, V} <- Fields],
    iolist_to_binary(["{", lists:join(",", Pairs), "}"]).

quote(V) when is_atom(V) -> quote(atom_to_binary(V));
quote(V) when is_binary(V) -> [$", << <<(escape(C))/binary>> || <<C>> <= V >>, $"].

escape($") -> <<"\\\"">>;
escape($\\) -> <<"\\\\">>;
escape($\n) -> <<"\\n">>;
escape($\r) -> <<"\\r">>;
escape($\t) -> <<"\\t">>;
escape(C) when C < 32 -> iolist_to_binary(io_lib:format("\\u~4.16.0b", [C]));
escape(C) -> <<C>>.
