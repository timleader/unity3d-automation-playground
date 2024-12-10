
-------------------------------------------------------------------------------
#TODO
- use http to setup the bridge
- use websocket url and headers to connect to the bridge
- all websocket traffic should be relayed to the peer
- need to know who is peer_a and who is peer_b

-------------------------------------------------------------------------------

https://localhost:8080/overview/    GET
https://localhost:8080/v1/connection/:bridge_id   GET | PUT | DELETE
    config
        - timeouts
        - etc...
        - bridge_id
        - name, description
        - token

ws://localhost:8080/ws/:bridge_id/  
    headers:
        - token
        - peer_id



`ws-relay`


DockerFile for easy deployment

cloudflare tunnel ??
