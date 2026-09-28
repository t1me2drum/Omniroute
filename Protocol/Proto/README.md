# Схеми protobuf

`ef_delta3.proto` і `ef_dp3.proto` взято з Android-версії (PowerHub), джерело — tolwi/hassio-ecoflow-cloud (Apache-2.0).
Додано лише `option csharp_namespace = "Omniroute.Protocol.Proto";`.

C#-класи в `Generated/` згенеровано заздалегідь. Після зміни схем їх треба перегенерувати (protoc 3.15+, наприклад із NuGet-пакета Grpc.Tools):

```bash
protoc --proto_path=Protocol/Proto --csharp_out=Protocol/Proto/Generated Protocol/Proto/ef_delta3.proto Protocol/Proto/ef_dp3.proto
```
