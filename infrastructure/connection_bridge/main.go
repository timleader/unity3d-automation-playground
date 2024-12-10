package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"

	"github.com/gorilla/mux"
	"github.com/gorilla/websocket"
)

// -----------------------------------------------------------------------------
var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	CheckOrigin:     func(r *http.Request) bool { return true },
}

// -----------------------------------------------------------------------------
func peerConnectionReader(bridge *Bridge, peer *Peer) { //	maybe a goroutine for each bridge would be better than per peer

	for {
		msgType, msg, err := peer.Connection.ReadMessage()
		if websocket.IsCloseError(err, 12) {
			break
		}
		if err != nil {
			log.Println(err)
			break
		}

		msgStr := string(msg)
		fmt.Printf("%v: %v\n", peer.Identifier, msgStr)

		fmt.Printf("bridge '%v' has %v peers.\n", bridge.Identifier, len(bridge.Peers))

		for _, v := range bridge.Peers {

			//	don't send message back to the sender
			if v.Identifier == peer.Identifier {
				continue
			}

			err = v.Connection.WriteMessage(msgType, msg)
			if err != nil {
				log.Println(err)
				continue
			}
		}
	}

	peer.Connection.Close()
}

// -----------------------------------------------------------------------------
var bridges []*Bridge

// -----------------------------------------------------------------------------
func homePage(w http.ResponseWriter, r *http.Request) {
	fmt.Fprintf(w, "Home Page")
}

// -----------------------------------------------------------------------------
func createBridgeHandler(w http.ResponseWriter, r *http.Request) {

	bridgeIdentifier := mux.Vars(r)["bridge_id"]

	var createBridgeRequest createBridgeRequest
	err := json.NewDecoder(r.Body).Decode(&createBridgeRequest)
	if err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	//	check if conflict with existing bridge
	bridge := &Bridge{
		Identifier:  bridgeIdentifier,
		Name:        createBridgeRequest.Name,
		Description: createBridgeRequest.Description,

		AccessKey: createBridgeRequest.AccessKey,

		MaxPeers: 2,
		Peers:    make([]*Peer, 0, 2),
	}

	for _, v := range bridges {
		if v.Identifier == bridgeIdentifier {
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}
	}

	bridges = append(bridges, bridge)

	createBridgeResponse := createBridgeResponse{
		Identifier:  bridge.Identifier,
		Name:        bridge.Name,
		Description: bridge.Description,
	}

	w.WriteHeader(http.StatusOK)
	w.Header().Set("Content-Type", "application/json")
	err = json.NewEncoder(w).Encode(createBridgeResponse)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	fmt.Printf("bridge '%v' created.\n", bridgeIdentifier)
}

// -----------------------------------------------------------------------------
func connectBridgeHandler(w http.ResponseWriter, r *http.Request) {

	bridgeIdentifier := mux.Vars(r)["bridge_id"]

	var bridge *Bridge
	for _, v := range bridges {
		if v.Identifier == bridgeIdentifier {
			bridge = v
			break
		}
	}

	if bridge == nil {
		http.Error(w, "Bridge not found", http.StatusNotFound)
		return
	}

	accessKey := r.Header.Get("x-relay-accesskey")
	peerUUID := r.Header.Get("x-relay-peeruuid")

	if accessKey != bridge.AccessKey {
		http.Error(w, "Invalid Access Key", http.StatusForbidden)
		return
	}

	// upgrade this connection to a WebSocket
	ws, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println(err)
		return
	}

	var peer *Peer

	connectionReplaced := false
	for _, v := range bridge.Peers {
		if v.Identifier == peerUUID {
			v.Connection.Close()
			v.Connection = ws
			peer = v
			connectionReplaced = true
			break
		}
	}

	if !connectionReplaced {

		peer = &Peer{
			Identifier: peerUUID,
			Connection: ws,
		}

		bridge.Peers = append(bridge.Peers, peer) //	does this need to be thread-safe?
	}

	fmt.Printf("peer %v : connected to bridge '%v'.\n", peer.Identifier, bridge.Identifier)

	go peerConnectionReader(bridge, peer)

	// Add peer as a connection to the bridge
	//    take note of peer_id, so that we can't connect a device to itself

}

// -----------------------------------------------------------------------------
func createRoutes() http.Handler {

	router := mux.NewRouter()

	router.HandleFunc("/", homePage).Methods("GET")
	router.HandleFunc("/overview", homePage).Methods("GET")

	//router.HandleFunc("/v1/connection/{bridge_id}", getBridgeHandler).Methods("GET")
	router.HandleFunc("/v1/connection/{bridge_id}", createBridgeHandler).Methods("PUT")
	//router.HandleFunc("/v1/connection/{bridge_id}", destroyBridgeHandler).Methods("DELETE")

	router.HandleFunc("/ws/{bridge_id}", connectBridgeHandler)

	return router
}

// -----------------------------------------------------------------------------
func main() {

	//	command line arguments for port number, etc...

	fmt.Println("Hello World")

	bridges = make([]*Bridge, 0, 16)

	router := createRoutes()
	log.Fatal(http.ListenAndServe(":8080", router))
}

// -----------------------------------------------------------------------------
type createBridgeRequest struct {
	Name        string `json:"name"`
	Description string `json:"description"`
	AccessKey   string `json:"access_key"`
	//ttl
}

// -----------------------------------------------------------------------------
type createBridgeResponse struct { //	successful or not
	Identifier  string `json:"identifier"`
	Name        string `json:"name"`
	Description string `json:"description"`
}

// -----------------------------------------------------------------------------
type Bridge struct {
	Identifier  string
	Name        string
	Description string

	AccessKey string

	MaxPeers int

	//	ttl
	//	close after no connections for x time
	//	close after only one connection for x time

	Peers []*Peer
}

// -----------------------------------------------------------------------------
type Peer struct {
	Identifier string
	Name       string
	Connection *websocket.Conn
}
