#!/bin/bash

cd $(dirname ${BASH_SOURCE[0]})

threads="$1"
lang="$2"

tmp_dir=$(mktemp -d)
trap "rm -rf \"$tmp_dir\"" EXIT

# テンプレートを今回使う設定ファイルとして一時ディレクトリにコピー
config_path="$tmp_dir/pacher_config.toml"
cp ./pahcer_config_template $config_path

# 引数で渡されたthreadsの値が不正ならpahcerのデフォルト値である0を設定する
echo $threads
if ! [[ "$threads" =~ ^[0-9]+$ ]]; then
  threads=0
fi

# threadsの設定を書き換える
sed -i "s|{THREADS}|$threads|g" $config_path

# inをout, errと同じ場所に配置して探しやすくする
if [ ! -d ./pahcer/in ]; then
  cp -r ./tools/in ./pahcer
fi

# 各言語でコンパイル・実行に関する設定を書き換える
case "$lang" in
  py|python)
    echo 'Python'
    sed -i 's|{DYN_COMPILE_CONFIG}|compile_steps = []|g' $config_path
    sed -i 's|{DYN_TEST_CONFIG}|"python", "../main.py",|g' $config_path
    ;;
  *)
    echo 'C#';
    sed -i 's|{DYN_COMPILE_CONFIG}|[[test.compile_steps]]\nprogram = "dotnet"\nargs = ["publish", "-c", "Release", "-o", "publish", "-v", "quiet"]|g' $config_path
    sed -i 's|{DYN_TEST_CONFIG}|"./publish/main"|g' $config_path
    ;;
esac

# 書き換えて完成したpahcer.config.tomlの内容を確認
# cat $config_path

# 一時ディレクトリにある設定ファイルを使ってpahcerを実行
pahcer run --setting-file $config_path
ls $tmp_dir

# out, errをpahcerが記録した現在時刻に対応させて手元にコピー
cur_date=$(cat './pahcer/summary.md' | tail -n 1 | awk '{print $1}' | xargs -I {} date -d {} +"%Y%m%d_%H%M%S")
mkdir -p pahcer/out
mkdir -p pahcer/err
cp -r tools/out pahcer/out/$cur_date
cp -r tools/err pahcer/err/$cur_date
