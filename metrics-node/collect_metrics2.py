import csv
import pandas as pd

def read_lighthouse_csv(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        reader = list(csv.reader(f))

    data_blocks = []
    block_start = 0

    for i, row in enumerate(reader):
        # Detect the start of new blocks by known headers
        if row == ["category", "score"] or row == ["category", "audit", "score", "displayValue", "description"]:
            if block_start < i:
                data_blocks.append(reader[block_start:i])
            block_start = i
    # Append the last block
    data_blocks.append(reader[block_start:])

    dfs = []
    for block in data_blocks:
        if len(block) < 2:
            continue  # skip empty or malformed blocks
        header = block[0]
        rows = block[1:]
        # Only include rows that match the header length
        valid_rows = [r for r in rows if len(r) == len(header)]
        df = pd.DataFrame(valid_rows, columns=header)
        dfs.append(df)

    return dfs