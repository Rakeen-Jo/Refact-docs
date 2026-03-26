# 07. CAN 구현

## 목표
- 제어 지연 최소화 + task starvation 방지
- 요청 기반 응답 구조와 주기 송신 구조의 충돌 제거

## 구현 포인트
- CAN task 주기 서비스 (`TASK_HAND_INFO_PERIOD` 기반)
- RX ISR에서 명령 수신/플래그 갱신
- task context에서 one-shot/periodic 처리 분리

## 중요 이슈 (실제 프로젝트 교훈)
1. **IRQ priority / FreeRTOS API 규칙**
   - FromISR API는 `configMAX_SYSCALL_INTERRUPT_PRIORITY` 경계 준수 필요
2. **TX mailbox 대기 방식**
   - busy-wait는 하위 task starvation 유발 가능
   - non-blocking/skip 전략으로 시스템 liveness 확보
3. **측정 지점 분리 필요**
   - FW 송신 지터 vs PC 수신/파싱 지터를 분리해야 원인 추적 가능

## 권장 지표
- FW 측: phase duration, tx success/fail, burst span
- PC 측: driver timestamp 기반 Δt, app parsing timestamp Δt
- 종합: p50/p90/p99/max + outlier 비율

## 운영 체크리스트
- [ ] CAN 연결 시 RED LED/task liveness 유지
- [ ] RX burst 상황에서도 term/system starvation 없음
- [ ] torque frame 누락 정책(허용/비허용)이 문서와 일치
