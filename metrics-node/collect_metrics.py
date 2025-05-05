import pandas as pd
import csv
import matplotlib.pyplot as plt

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


def plot_category_scores(df, output_path="category_scores.png"):
    df['score'] = df['score'].astype(float)
    plt.figure(figsize=(8, 5))
    plt.bar(df['category'], df['score'], color='skyblue')
    plt.ylim(0, 1.1)
    plt.title('Lighthouse Category Scores')
    plt.ylabel('Score')
    plt.xlabel('Category')
    plt.grid(axis='y', linestyle='--', alpha=0.7)
    plt.tight_layout()
    plt.savefig(output_path, dpi=300, bbox_inches='tight')
    plt.close()

def plot_audit_scores(df, output_prefix="audit_scores"):
    df['score'] = pd.to_numeric(df['score'], errors='coerce')
    grouped = df.groupby('category')
    for category, group in grouped:
        plt.figure(figsize=(10, 5))
        plt.bar(group['audit'], group['score'], color='mediumseagreen')
        plt.ylim(0, 1.1)
        plt.title(f'{category.capitalize()} Audit Metrics')
        plt.ylabel('Score')
        plt.xlabel('Audit')
        plt.xticks(rotation=45, ha='right')
        plt.grid(axis='y', linestyle='--', alpha=0.7)
        plt.tight_layout()
        filename = f"{output_prefix}_{category}.png"
        plt.savefig(filename, dpi=300, bbox_inches='tight')
        plt.close()

# --- Main execution ---
#file_path = 'reports/blazor-server-desktop/report.csv'  
file_path = 'reports/react-dev-desktop/report.csv'  
dfs = read_lighthouse_csv(file_path)

# Assume first block is category scores, second block is audit scores
for df in dfs:
    if set(df.columns) == {"category", "score"}:
        plot_category_scores(df)
    elif set(df.columns).issuperset({"category", "audit", "score"}):
        plot_audit_scores(df)