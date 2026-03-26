# 08. TCP 구현 및 프로토콜

## 설계 목표
- PC 요청 시점에 최신 상태를 반환하는 요청/응답형 구조
- Motor loop와 TCP 처리의 결합도 최소화
- 디버그/운영 중에도 연결 안정성 유지

## 구현 요약
- TCP server task에서 세션 관리, packet parsing, response serialization 담당
- 내부 상태는 motor/sensor 최신 버퍼를 참조해 응답 생성
- 장시간 blocking I/O로 하위 task가 굶지 않도록 timeout/주기 flush 전략 사용

## 프로토콜 문서화 권장 항목
1. Header(길이/타입/시퀀스)
2. Payload(모터 각도/전류/센서/raw or scaled)
3. 에러코드/재시도 규칙
4. 버전 필드(하위호환)

## 실무 운영 포인트
- TCP 연결 직후 burst 요청에서 응답 지연이 커지면
  - task priority
  - send buffer
  - term_print 출력량
  먼저 점검

## 검증 템플릿
| 항목 | 측정값 | 기준 | 결과 |
|---|---:|---:|---|
| connect latency |  |  |  |
| req->resp 평균 |  |  |  |
| req->resp max |  |  |  |
| disconnect/reconnect 안정성 |  |  |  |
