# 11. Bug Timeline & Fix Map

## 타임라인 (핵심)
| 시점 | 증상 | 원인(요약) | 수정 | 커밋 | 결과 |
|---|---|---|---|---|---|
| 초기 Stage2 | Motor loop 이후 시스템 굶김, LED 정지 | 스케줄/블로킹/critical 구간 누적 | 계측/가드 추가 | e8b4a28 | 원인 분리 진행 |
| 이후 | init 구간 지연/출력 병목 | HAL_Delay busy-wait 영향 | HAL_Delay -> osDelay | d47549f | 부분 개선 |
| 진단 단계 | 서비스 루프 병목 의심 | drain/TX 경로 분리 필요 | bypass/test 매크로 | abe16ad, 3323042 | 병목 범위 축소 |
| CAN 연결 시 freeze | ISR/API/priority 경계 이슈 | IRQ priority 조정 | CAN IRQ priority 8->5 | 6bd5aa8 | assert성 freeze 완화 |
| CAN 시 System starvation | CAN TX busy-wait 영향 | non-blocking mailbox 정책 | 8f43ee0 | starvation 개선 |
| ESP sensor 무응답 | 요청은 오지만 반영 안됨 | half-duplex TX race/타이밍 | TX_DONE stale clear 등 | c5379b7, cce9118, 69f9b6a | SRdone 회복/튜닝 진행 |

## 최근 ESP-IDF 센서 펌웨어 커밋
- `2cbe7dd`: turnaround delay + TX_DONE lost guard
- `b394785`: LED ISR request debug 모드
- `fdd11d6`: BLUE 패턴 세분화(TX_DONE 추적)
- `c5379b7`: stale TX_DONE clear
- `cce9118`: explicit reply delay, LED debug off
- `69f9b6a`: late-cut(500us) 추가

## 재발 방지 규칙
- IRQ priority/FromISR 규칙은 문서화 + 코드 리뷰 체크리스트에 포함
- busy-wait는 상위 priority task/ISR에서 금지(필요 시 짧은 bounded only)
- 누적 카운터는 interval delta와 함께 본다
