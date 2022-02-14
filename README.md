# doppler-easynetq-hosepipe-worker

Worker to republish messages that caused an error being processed by a consumer

## Settings

### RabbitMQ Connections

#### By appsettings.json

```json
  "HosepipeSettings": {
    "Connections": {
      "localhost": {
        "ConnectionString": "host=localhost"
      },
      "example-1": {
        "ConnectionString": "host=example-server;virtualHost=example-virtual;username=guest;password=guest"
      },
      "example-2": {
        "ConnectionString": "host=example-server;virtualHost=example-virtual;username=guest"
        "SecretPassword": "someSecretPassword",
      },
    }
  }
```

#### By environment variable

```env
"HosepipeSettings__Connections__localhost__ConnectionString": "host=localhost"
"HosepipeSettings__Connections__example-1__ConnectionString": "host=example-server;virtualHost=example-virtual;username=guest;password=guest"
"HosepipeSettings__Connections__example-2__ConnectionString": "host=example-server;virtualHost=example-virtual;username=guest"
"HosepipeSettings__Connections__example-2__SecretPassword": "someSecretPassword"
```