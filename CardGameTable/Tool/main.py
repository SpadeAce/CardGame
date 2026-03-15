"""
main.py
-------
Excel → Protobuf → Unity 바이너리 자동화 파이프라인의 진입점.

사용법:
  python main.py                          # 기본 실행 (config.yaml 사용)
  python main.py --config other.yaml      # 설정 파일 경로 오버라이드
  python main.py --skip-copy              # Unity 경로 복사 단계 건너뜀
  python main.py --schema-dir ./Proto/    # 스키마 디렉터리 오버라이드 (멀티 파일 모드)
  python main.py --data-dir ./Table/      # 데이터 디렉터리 오버라이드 (멀티 파일 모드)
  python main.py --excel path/to.xlsx     # 단일 파일 모드 오버라이드 (레거시)

동작 모드 결정 우선순위:
  1. --excel 인수 지정 시           → 단일 파일 모드
  2. config에 excel_path 있을 때    → 단일 파일 모드
  3. config에 schema_dir/data_dir   → 멀티 파일 모드 (기본)

파이프라인 단계:
  1. config.yaml 로드
  2. Excel 파싱 (단일/멀티 파일 모드 자동 선택)
  3. 스키마 / 데이터 검증
  4. 스키마 스냅샷 비교 (필드 번호 변경 경고)
  5. .proto 파일 생성 + 스냅샷 저장
  6. protoc 실행 → _pb2.py + .cs 생성
  7. 데이터 직렬화 → .bytes 생성
  8. .cs / .bytes Unity 프로젝트 경로로 복사
"""

from __future__ import annotations

import argparse
import shutil
import sys
import traceback
from pathlib import Path

import yaml


TOOL_DIR = Path(__file__).parent.resolve()


# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------

def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Excel → Protobuf → Unity 바이너리 자동화 파이프라인",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "예시:\n"
            "  python main.py\n"
            "  python main.py --skip-copy\n"
            "  python main.py --schema-dir ../Table/Proto/ --data-dir ../Table/\n"
            "  python main.py --excel ../Table/GameData.xlsx  # 레거시 단일 파일 모드\n"
        ),
    )
    parser.add_argument(
        "--config",
        metavar="PATH",
        default=str(TOOL_DIR / "config.yaml"),
        help="설정 파일 경로 (기본값: ./config.yaml)",
    )
    parser.add_argument(
        "--skip-copy",
        action="store_true",
        help="생성된 파일을 Unity 프로젝트로 복사하는 단계를 건너뜀",
    )
    # 멀티 파일 모드 오버라이드
    parser.add_argument(
        "--schema-dir",
        metavar="PATH",
        help="스키마 파일 디렉터리 오버라이드 (Proto_*.xlsx 위치)",
    )
    parser.add_argument(
        "--data-dir",
        metavar="PATH",
        help="데이터 파일 디렉터리 오버라이드 (*.xlsx 위치)",
    )
    # 단일 파일 모드 오버라이드 (레거시)
    parser.add_argument(
        "--excel",
        metavar="PATH",
        help="단일 Excel 파일 경로 (레거시 단일 파일 모드)",
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Step helpers
# ---------------------------------------------------------------------------

def _step(num: int, total: int, desc: str) -> None:
    print(f"\n[{num}/{total}] {desc}")


def _ok(msg: str = "") -> None:
    if msg:
        print(f"  ✓ {msg}")


def _warn(msg: str) -> None:
    print(f"  ⚠ {msg}")


# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

def run_pipeline(args: argparse.Namespace) -> None:
    """전체 파이프라인을 순차적으로 실행한다."""
    TOTAL_STEPS = 8

    # ------------------------------------------------------------------
    # Step 1: config.yaml 로드
    # ------------------------------------------------------------------
    _step(1, TOTAL_STEPS, "config.yaml 로드")
    config_path = Path(args.config)
    if not config_path.exists():
        raise FileNotFoundError(f"설정 파일을 찾을 수 없습니다: {config_path}")

    with open(config_path, encoding="utf-8") as f:
        config = yaml.safe_load(f)

    required_keys = ("protoc_path", "unity_cs_output", "unity_bytes_output", "package_name")
    missing = [k for k in required_keys if k not in config]
    if missing:
        raise ValueError(f"config.yaml에 필수 키가 없습니다: {missing}")

    # 동작 모드 결정
    # 우선순위: --excel 인수 > config.excel_path > config.schema_dir+data_dir
    use_single_file = bool(args.excel or config.get("excel_path"))

    _ok(f"설정 로드 완료: {config_path}")
    print(f"  package_name      : {config['package_name']}")
    if use_single_file:
        print(f"  모드              : 단일 파일 모드 (레거시)")
        print(f"  excel_path        : {args.excel or config.get('excel_path')}")
    else:
        print(f"  모드              : 멀티 파일 모드")
        print(f"  schema_dir        : {args.schema_dir or config.get('schema_dir')}")
        print(f"  data_dir          : {args.data_dir or config.get('data_dir')}")
    print(f"  unity_cs_output   : {config['unity_cs_output']}")
    print(f"  unity_bytes_output: {config['unity_bytes_output']}")

    # ------------------------------------------------------------------
    # Step 2: Excel 파싱
    # ------------------------------------------------------------------
    if use_single_file:
        _step(2, TOTAL_STEPS, "Excel 파싱 (단일 파일 모드)")
        from excel_parser import parse_excel
        excel_path = TOOL_DIR / (args.excel if args.excel else config["excel_path"])
        excel_data = parse_excel(excel_path)
        _ok(f"파싱 완료: {excel_path}")
    else:
        _step(2, TOTAL_STEPS, "Excel 파싱 (멀티 파일 모드)")
        from excel_parser import parse_excel_multi

        raw_schema_dir = args.schema_dir or config.get("schema_dir")
        raw_data_dir   = args.data_dir   or config.get("data_dir")

        if not raw_schema_dir or not raw_data_dir:
            raise ValueError(
                "멀티 파일 모드에서는 config.yaml에 schema_dir과 data_dir이 필요합니다.\n"
                "또는 --schema-dir / --data-dir 인수를 사용하세요."
            )

        schema_dir = (TOOL_DIR / raw_schema_dir).resolve()
        data_dir   = (TOOL_DIR / raw_data_dir).resolve()
        excel_data = parse_excel_multi(schema_dir, data_dir)
        _ok(f"파싱 완료 — schema_dir: {schema_dir}")
        _ok(f"           data_dir  : {data_dir}")

    print(f"  메시지     : {list(excel_data.schema.keys())}")
    print(f"  Enum       : {list(excel_data.enums.keys())}")
    print(f"  데이터 시트: {list(excel_data.data.keys())}")

    # ------------------------------------------------------------------
    # Step 3: 스키마 / 데이터 검증
    # ------------------------------------------------------------------
    _step(3, TOTAL_STEPS, "스키마 / 데이터 검증")
    from validator import validate, ValidationError

    validate(excel_data)
    _ok("검증 통과")

    # ------------------------------------------------------------------
    # Step 4: 스키마 스냅샷 비교
    # ------------------------------------------------------------------
    _step(4, TOTAL_STEPS, "스키마 스냅샷 비교 (필드 번호 변경 감지)")
    from proto_generator import load_snapshot, compare_snapshot, save_snapshot, generate_proto

    snapshot = load_snapshot(TOOL_DIR)
    warnings = compare_snapshot(excel_data, snapshot)
    if warnings:
        for w in warnings:
            print(w)
    else:
        _ok("필드 번호 변경 없음")

    # ------------------------------------------------------------------
    # Step 5: .proto 파일 생성 + 스냅샷 저장
    # ------------------------------------------------------------------
    _step(5, TOTAL_STEPS, ".proto 파일 생성")
    proto_path = generate_proto(excel_data, config, TOOL_DIR)
    save_snapshot(excel_data, TOOL_DIR)
    _ok(f"생성: {proto_path}")

    # ------------------------------------------------------------------
    # Step 6: protoc 실행
    # ------------------------------------------------------------------
    _step(6, TOTAL_STEPS, "protoc 실행 (C# 및 Python 코드 생성)")
    from cs_generator import run_protoc

    run_protoc(config, TOOL_DIR)
    cs_out_dir = TOOL_DIR / "output" / "cs"
    py_out_dir = TOOL_DIR / "output" / "py"
    cs_files = list(cs_out_dir.glob("*.cs"))
    py_files = list(py_out_dir.glob("*_pb2.py"))
    _ok(f"C# 파일: {[f.name for f in cs_files]}")
    _ok(f"Python pb2 파일: {[f.name for f in py_files]}")

    # ------------------------------------------------------------------
    # Step 7: 데이터 직렬화 → .bytes
    # ------------------------------------------------------------------
    _step(7, TOTAL_STEPS, "데이터 직렬화 → .bytes")
    from data_serializer import serialize_all

    bytes_files = serialize_all(excel_data, config, TOOL_DIR)
    for bf in bytes_files:
        _ok(f"직렬화: {Path(bf).name}")

    # ------------------------------------------------------------------
    # Step 8: Unity 경로로 복사
    # ------------------------------------------------------------------
    _step(8, TOTAL_STEPS, "Unity 프로젝트로 파일 복사")
    if args.skip_copy:
        print("  --skip-copy 옵션으로 인해 복사를 건너뜁니다.")
    else:
        cs_src    = TOOL_DIR / "output" / "cs"
        cs_dst    = (TOOL_DIR / config["unity_cs_output"]).resolve()
        bytes_src = TOOL_DIR / "output" / "bytes"
        bytes_dst = (TOOL_DIR / config["unity_bytes_output"]).resolve()

        cs_dst.mkdir(parents=True, exist_ok=True)
        bytes_dst.mkdir(parents=True, exist_ok=True)

        cs_copied: list[str] = []
        for cs_file in cs_src.glob("*.cs"):
            shutil.copy2(cs_file, cs_dst / cs_file.name)
            cs_copied.append(cs_file.name)

        bytes_copied: list[str] = []
        for b_file in bytes_src.glob("*.bytes"):
            shutil.copy2(b_file, bytes_dst / b_file.name)
            bytes_copied.append(b_file.name)

        _ok(f"C# 복사 → {cs_dst}")
        for f in cs_copied:
            print(f"    {f}")
        _ok(f".bytes 복사 → {bytes_dst}")
        for f in bytes_copied:
            print(f"    {f}")

    # ------------------------------------------------------------------
    # 요약 리포트
    # ------------------------------------------------------------------
    print("\n" + "=" * 60)
    print("  실행 완료 요약")
    print("=" * 60)
    print(f"  처리한 데이터 시트 수  : {len(excel_data.data)}")
    print(f"  정의된 메시지 수       : {len(excel_data.schema)}")
    print(f"  정의된 Enum 수         : {len(excel_data.enums)}")
    print(f"  생성된 .bytes 파일 수  : {len(bytes_files)}")
    print(f"  생성된 .proto 경로     : {proto_path}")
    if not args.skip_copy:
        print(f"  Unity C# 출력 경로    : {cs_dst}")
        print(f"  Unity .bytes 출력 경로: {bytes_dst}")
    if warnings:
        print(f"\n  ⚠ 필드 번호 변경 경고: {len(warnings)}건")
        for w in warnings:
            print(f"  {w.strip()}")
    print("=" * 60)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    """CLI 진입점."""
    args = _parse_args()

    # Tool 디렉터리를 sys.path에 추가하여 모듈 import 보장
    tool_dir_str = str(TOOL_DIR)
    if tool_dir_str not in sys.path:
        sys.path.insert(0, tool_dir_str)

    try:
        run_pipeline(args)
    except KeyboardInterrupt:
        print("\n중단되었습니다.", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"\n오류 발생: {e}", file=sys.stderr)
        if "--debug" in sys.argv:
            traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
