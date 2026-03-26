# 08. TCP 구현 및 프로토콜

## 설계 목표
- PC 요청 시점의 최신 상태를 반환 (request/response)
- Motor loop(3ms)와 TCP 처리의 결합 최소화
- 연결 유지/재연결/버스트 요청에서 안정성 확보

## 구현 원칙
1. TCP task는 네트워크 I/O와 packet 경계 처리 담당
2. 상태 데이터는 motor/sensor 최신 버퍼를 snapshot으로 응답
3. blocking 구간은 bounded timeout으로 제한
4. 과도한 debug print는 term_print 경로에서 제어

## 프로토콜 문서화 항목(필수)
- Header: magic/version/type/len/seq
- Payload: joint pos/current/sensor/error
- Direction:
  - PC -> FW: 제어 명령(토크 등)
  - FW -> PC: 상태 응답/진단
- Error code: parse fail / invalid len / timeout
- Versioning: 하위호환 규칙

## 운영 중 흔한 문제와 대응
- 증상: 접속 후 응답 느림
  - 점검: task priority, print volume, lwIP thread context
- 증상: burst 요청 시 latency spike
  - 점검: send buffer, copy 횟수, blocking wait 존재 여부
- 증상: 연결은 유지되나 stale 응답
  - 점검: snapshot 시점, sequence 검증

## 검증 체크리스트
- [ ] connect/disconnect 반복 100회 이상 안정
- [ ] req->resp 평균/최대 지연 기록
- [ ] CAN 모드 전환 시 TCP task starvation 없음
- [ ] monitor enabled 상태에서도 protocol 깨짐 없음

## 기록 템플릿
| 항목 | 측정값 | 기준 | Pass/Fail | 비고 |
|---|---:|---:|---|---|
| connect latency |  |  |  |  |
| req->resp avg |  |  |  |  |
| req->resp p99 |  |  |  |  |
| req->resp max |  |  |  |  |
| reconnect 안정성 |  |  |  |  |
