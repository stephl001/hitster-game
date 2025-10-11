import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.connectionPromise = null
  }

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection
    }

    if (this.connectionPromise) {
      return this.connectionPromise
    }

    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000'

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/gameHub`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    this.connectionPromise = this.connection
      .start()
      .then(() => {
        console.log('SignalR Connected')
        this.connectionPromise = null
        return this.connection
      })
      .catch((err) => {
        console.error('SignalR Connection Error:', err)
        this.connectionPromise = null
        throw err
      })

    return this.connectionPromise
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
  }

  on(eventName, callback) {
    if (this.connection) {
      this.connection.on(eventName, callback)
    }
  }

  off(eventName, callback) {
    if (this.connection) {
      this.connection.off(eventName, callback)
    }
  }

  async invoke(methodName, ...args) {
    if (!this.connection) {
      throw new Error('Connection not established')
    }
    return await this.connection.invoke(methodName, ...args)
  }

  getConnectionState() {
    return this.connection?.state || 'Disconnected'
  }
}

export default new SignalRService()
