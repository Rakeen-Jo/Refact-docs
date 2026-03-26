# 07. CAN 구현

## 요구사항
- 3ms 제어 문맥에서 지연/충돌 최소화
- 제어 프레임 누락 정책과 latency 사이 trade-off 관리

## 주요 이슈/교훈
1. IRQ priority와 FreeRTOS FromISR API 경계
2. CAN TX mailbox 대기 방식(busy-wait vs non-blocking)
3. 통신 task가 System/Term task를 굶기지 않도록 설계

## 안정화 포인트
- CAN ISR에서 사용하는 API가 IRQ priority 정책에 맞는지 검증
- mailbox full 시 장시간 busy-wait 지양
- 요청 기반 처리 시도와 주기 스트림 처리의 역할 분리

## 지표
- 수신/송신 gap 통계
- burst/span 통계
- pos2first_us, tx_span_us (PC 연동 측정)

## 남은 운영 체크
- [ ] 최대 지연(max spike) 원인이 PC 수신 측인지 FW 송신 측인지 분리 로그 유지
- [ ] CAN 연결 시 task starvation/LED freeze 재발 여부 회귀 테스트
