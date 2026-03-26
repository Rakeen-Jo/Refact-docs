# 03. Task / Scheduler 구성

## Task 목록 (Refact_2ea0133 기준)
| Task | Priority | Period | 역할 |
|---|---:|---|---|
| System | osPriorityBelowNormal | 주기성 service | 모니터, 상태 점검, 시스템 서비스 |
| CAN Comm | osPriorityNormal | 1ms (`TASK_HAND_INFO_PERIOD`) | CAN 요청/응답, 스트림/진단 |
| Motor Ctrl | osPriorityNormal | 3ms (`MOTOR_CTRL_TASK_PERIOD_MS`) | 모터 제어/상태 갱신 |
| TCP Server | osPriorityNormal | event-driven | TCP 통신/명령 처리 |
| Term Print | osPriorityBelowNormal1 | 주기 flush | 디버그 출력/터미널 |

> 실제 CMSIS→FreeRTOS 매핑 숫자는 빌드 설정에 따름. 문서에는 상대 우선순위 중심으로 관리.

## 스케줄링 정책 요약
- Motor loop는 `vTaskDelayUntil` 기반 고정 주기 유지
- 통신 경로(CAN/TCP)는 요청 기반 처리로 Motor loop 간섭 최소화
- 블로킹 위험 구간(CAN mailbox 대기, 장시간 critical section) 최소화

## 핵심 이슈
1. **IRQ priority vs FreeRTOS FromISR API 제약**
   - `configMAX_SYSCALL_INTERRUPT_PRIORITY` 경계 위반 시 assert/lockup 위험
2. **busy-wait 구간**
   - 상위 priority task에서 busy-wait가 길면 하위 task starvation
3. **critical section 장기 점유**
   - SysTick/ETH 등 지연 유발 가능

## 운영 체크리스트
- [ ] CAN IRQ, DMA/UART IRQ 우선순위가 MAX_SYSCALL 정책과 일치하는가
- [ ] taskENTER_CRITICAL/EXIT 쌍이 모든 경로(early return 포함)에서 보장되는가
- [ ] 통신 task에서 장시간 while-polling이 없는가
