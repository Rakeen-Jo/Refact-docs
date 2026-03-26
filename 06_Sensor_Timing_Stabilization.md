# 06. Sensor Timing Stabilization

## 목표
- sensor read 성공률 향상
- timeout/cut/dead 최소화
- 포트별 편차(U1/U3/U4/U6) 관리

## 관측된 패턴
- U3/U4/U6는 `SRtx≈SRdone`, `Stmo/cut/dead≈0`로 안정화되는 구간 확인
- U1은 상대적으로 `Stmo/cut/dead/UErr` 누적이 크게 발생하는 시점 존재

## 원인 후보
1. U1 물리 링크 품질(배선/그라운드/트랜시버 편차)
2. half-duplex turnaround 타이밍 미스
3. 늦은 응답이 다음 cycle과 충돌하는 케이스

## 적용된 대응 (ESP-IDF 센서 펌웨어 측)
- TX_DONE stale clear
- turnaround delay 보강
- explicit reply delay (`REPLY_DELAY_US`)
- late-cut (`REPLY_LATE_US`)
- TX_DONE lost/stuck guard

## 해석 가이드
- `Sid`는 마지막 성공 sensor ID이므로 포트별로 다르게 찍혀도 정상
- `dead`는 단순 timeout이 아니라 RX deadlock watchdog 개입 카운터
- `UErr`와 `dead`가 같이 증가하면 recover 타이밍 꼬임 가능성 큼

## 포트별 기록 템플릿
| Port | SRtx | SRdone | Stmo | cut | dead | UErr | 비고 |
|---|---:|---:|---:|---:|---:|---:|---|
| U1 |  |  |  |  |  |  |  |
| U3 |  |  |  |  |  |  |  |
| U4 |  |  |  |  |  |  |  |
| U6 |  |  |  |  |  |  |  |
